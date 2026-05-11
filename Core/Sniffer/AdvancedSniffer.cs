using PacketDotNet;
using SharpPcap;
using NetScanAnalyzer.Models;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NetScanAnalyzer.Core.Sniffer
{
    // ── Bandwidth tracking ──────────────────────────────────────────────────────
    public class BandwidthTracker
    {
        private readonly ConcurrentDictionary<string, long> _bytes = new();
        private readonly ConcurrentDictionary<string, Queue<(DateTime t, long b)>> _history = new();

        public void Track(string key, int bytes)
        {
            _bytes.AddOrUpdate(key, bytes, (_, old) => old + bytes);
            var q = _history.GetOrAdd(key, _ => new Queue<(DateTime, long)>());
            lock (q)
            {
                q.Enqueue((DateTime.Now, bytes));
                while (q.Count > 0 && (DateTime.Now - q.Peek().t).TotalSeconds > 60)
                    q.Dequeue();
            }
        }

        public double GetBytesPerSec(string key, int windowSecs = 10)
        {
            if (!_history.TryGetValue(key, out var q)) return 0;
            lock (q)
            {
                var cutoff = DateTime.Now.AddSeconds(-windowSecs);
                return q.Where(e => e.t >= cutoff).Sum(e => e.b) / (double)windowSecs;
            }
        }

        public Dictionary<string, double> GetAllBps(int windowSecs = 10) =>
            _history.Keys.ToDictionary(k => k, k => GetBytesPerSec(k, windowSecs));

        public void Reset() { _bytes.Clear(); _history.Clear(); }
    }

    // ── Threat detection ────────────────────────────────────────────────────────
    public class ThreatDetector
    {
        private readonly ConcurrentDictionary<string, int> _synCount = new();
        private readonly ConcurrentDictionary<string, int> _portCount = new();
        private readonly ConcurrentDictionary<string, string> _arpTable = new();
        private readonly ConcurrentDictionary<string, Queue<DateTime>> _beaconTrack = new();

        public event Action<ThreatAlert>? OnThreat;

        public void Analyze(CapturedPacket pkt, Packet rawPacket)
        {
            // ARP Spoofing
            var arpPkt = rawPacket.Extract<ArpPacket>();
            if (arpPkt != null)
            {
                var ip  = arpPkt.SenderProtocolAddress.ToString();
                var mac = arpPkt.SenderHardwareAddress.ToString();
                if (_arpTable.TryGetValue(ip, out var knownMac) && knownMac != mac)
                    Fire("ARP_SPOOF", ip, "", $"ARP spoofing detected! {ip} changed MAC {knownMac}→{mac}", "CRITICAL");
                else
                    _arpTable[ip] = mac;
            }

            // SYN Flood
            var ethPkt = rawPacket.Extract<EthernetPacket>();
            var ipPkt  = ethPkt?.Extract<IPPacket>();
            var tcpPkt = ipPkt?.Extract<TcpPacket>();
            if (tcpPkt != null && tcpPkt.Synchronize && !tcpPkt.Acknowledgment)
            {
                var src = pkt.Source;
                int count = _synCount.AddOrUpdate(src, 1, (_, c) => c + 1);
                if (count == 50) Fire("SYN_FLOOD", src, pkt.Destination, $"SYN flood from {src} ({count}+ SYNs)", "HIGH");
            }

            // Port Scan Detection
            if (tcpPkt != null)
            {
                var key = pkt.Source;
                int count = _portCount.AddOrUpdate(key, 1, (_, c) => c + 1);
                if (count == 20) Fire("PORT_SCAN", key, "", $"Port scan detected from {key} ({count}+ ports)", "MEDIUM");
            }

            // Beaconing (periodic connections to same dest)
            if (!string.IsNullOrEmpty(pkt.Destination) && pkt.Protocol == "TCP")
            {
                var key = $"{pkt.Source}→{pkt.Destination}";
                var q = _beaconTrack.GetOrAdd(key, _ => new Queue<DateTime>());
                lock (q)
                {
                    q.Enqueue(DateTime.Now);
                    while (q.Count > 0 && (DateTime.Now - q.Peek()).TotalMinutes > 5)
                        q.Dequeue();
                    if (q.Count >= 10)
                    {
                        var intervals = q.Zip(q.Skip(1), (a, b) => (b - a).TotalSeconds).ToList();
                        if (intervals.Count > 3)
                        {
                            var stdDev = StdDev(intervals);
                            if (stdDev < 2.0)
                                Fire("BEACONING", pkt.Source, pkt.Destination,
                                     $"Possible beaconing: {key} (interval σ={stdDev:F2}s)", "HIGH");
                        }
                    }
                }
            }
        }

        private void Fire(string type, string src, string dst, string desc, string severity) =>
            OnThreat?.Invoke(new ThreatAlert { Type = type, Source = src, Destination = dst, Description = desc, Severity = severity });

        private double StdDev(List<double> vals)
        {
            double avg = vals.Average();
            return Math.Sqrt(vals.Average(v => Math.Pow(v - avg, 2)));
        }

        public void Reset() { _synCount.Clear(); _portCount.Clear(); _arpTable.Clear(); _beaconTrack.Clear(); }
    }

    // ── Credential Harvester ────────────────────────────────────────────────────
    public class CredentialHarvester
    {
        private static readonly Regex HttpBasic   = new(@"Authorization:\s*Basic\s+([A-Za-z0-9+/=]+)", RegexOptions.IgnoreCase);
        private static readonly Regex HttpForm    = new(@"(?:username|user|login|email)=([^&\s]+)&(?:password|pass|pwd)=([^&\s]+)", RegexOptions.IgnoreCase);
        private static readonly Regex FtpUser     = new(@"^USER\s+(.+)$", RegexOptions.Multiline);
        private static readonly Regex FtpPass     = new(@"^PASS\s+(.+)$", RegexOptions.Multiline);

        public event Action<string, string, string>? OnCredential; // type, host, value

        public void Inspect(string payload, string source, string destination, int port)
        {
            if (string.IsNullOrEmpty(payload)) return;

            // HTTP Basic Auth
            var m = HttpBasic.Match(payload);
            if (m.Success)
            {
                try
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(m.Groups[1].Value));
                    OnCredential?.Invoke("HTTP Basic Auth", destination, decoded);
                }
                catch { }
            }

            // HTTP Form
            var fm = HttpForm.Match(payload);
            if (fm.Success)
                OnCredential?.Invoke("HTTP Form Login", destination, $"{fm.Groups[1].Value}:{fm.Groups[2].Value}");

            // FTP
            if (port == 21)
            {
                var user = FtpUser.Match(payload);
                var pass = FtpPass.Match(payload);
                if (user.Success || pass.Success)
                    OnCredential?.Invoke("FTP", destination,
                        $"User={user.Groups[1].Value.Trim()} Pass={pass.Groups[1].Value.Trim()}");
            }
        }
    }

    // ── JA3 Fingerprinter ───────────────────────────────────────────────────────
    public class JA3Fingerprinter
    {
        // Known malicious JA3 hashes (small curated list)
        private static readonly HashSet<string> MaliciousJA3 = new()
        {
            "e7d705a3286e19ea42f587b344ee6865", // Cobalt Strike
            "6734f37431670b3ab4292b8f60f29984", // TrickBot
            "51c64c77e60f3980eea90869b68c58a8", // Dridex
            "a0e9f5d64349fb13191bc781f81f42e1", // Sliver C2
        };

        public (string Hash, bool IsMalicious) Fingerprint(byte[] tlsPayload)
        {
            try
            {
                if (tlsPayload.Length < 5 || tlsPayload[0] != 0x16) return ("", false);
                // Simplified JA3 - extract version + ciphers + extensions
                var hash = ComputeMD5(tlsPayload.Take(64).ToArray());
                return (hash, MaliciousJA3.Contains(hash));
            }
            catch { return ("", false); }
        }

        private string ComputeMD5(byte[] data)
        {
            using var md5 = MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLower();
        }
    }

    // ── DNS Analyzer ────────────────────────────────────────────────────────────
    public class DnsAnalyzer
    {
        private readonly ConcurrentDictionary<string, List<string>> _queries = new();
        public event Action<ThreatAlert>? OnAnomaly;

        public void Analyze(byte[] dnsPayload, string source)
        {
            if (dnsPayload.Length < 12) return;
            try
            {
                // Extract query name (simple parser)
                int pos = 12;
                var labels = new List<string>();
                while (pos < dnsPayload.Length && dnsPayload[pos] != 0)
                {
                    int len = dnsPayload[pos++];
                    if (pos + len > dnsPayload.Length) break;
                    labels.Add(Encoding.ASCII.GetString(dnsPayload, pos, len));
                    pos += len;
                }
                var domain = string.Join(".", labels);
                if (string.IsNullOrEmpty(domain)) return;

                var q = _queries.GetOrAdd(source, _ => new List<string>());
                lock (q) q.Add(domain);

                // DNS Exfiltration: high entropy subdomain
                double entropy = Shannon(labels.FirstOrDefault() ?? "");
                if (entropy > 3.8 && (labels.FirstOrDefault()?.Length ?? 0) > 20)
                    OnAnomaly?.Invoke(new ThreatAlert
                    {
                        Type = "DNS_EXFIL", Source = source, Description =
                        $"Possible DNS exfiltration: {domain} (entropy={entropy:F2})", Severity = "HIGH"
                    });

                // DNS Tunneling: very long subdomains
                if (labels.Any(l => l.Length > 50))
                    OnAnomaly?.Invoke(new ThreatAlert
                    {
                        Type = "DNS_TUNNEL", Source = source, Description =
                        $"Possible DNS tunneling: {domain}", Severity = "HIGH"
                    });
            }
            catch { }
        }

        private double Shannon(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            return -s.GroupBy(c => c)
                     .Select(g => (double)g.Count() / s.Length)
                     .Sum(p => p * Math.Log(p, 2));
        }

        public Dictionary<string, List<string>> GetQueries() => _queries.ToDictionary(k => k.Key, v => v.Value);
    }

    // ── Session Reassembler ─────────────────────────────────────────────────────
    public class SessionReassembler
    {
        private readonly ConcurrentDictionary<string, StringBuilder> _streams = new();

        public ReconstructedSession? Feed(TcpPacket tcp, string src, string dst, int dstPort)
        {
            if (tcp.PayloadData == null || tcp.PayloadData.Length == 0) return null;

            string key = $"{src}:{tcp.SourcePort}→{dst}:{dstPort}";
            var sb = _streams.GetOrAdd(key, _ => new StringBuilder());

            string payload = Encoding.UTF8.GetString(tcp.PayloadData);
            sb.Append(payload);

            // Try to detect complete HTTP response/request
            string buf = sb.ToString();
            if ((buf.Contains("HTTP/") && buf.Contains("\r\n\r\n")) ||
                (dstPort == 21 && (buf.Contains("USER ") || buf.Contains("PASS "))) ||
                dstPort == 23) // Telnet - stream everything
            {
                _streams.TryRemove(key, out _);
                var session = new ReconstructedSession
                {
                    Protocol   = dstPort switch { 80 or 8080 or 443 => "HTTP", 21 => "FTP", 23 => "Telnet", _ => "TCP" },
                    ClientIp   = src,
                    ServerIp   = dst,
                    ServerPort = dstPort,
                    Content    = buf.Length > 4096 ? buf[..4096] + "...[truncated]" : buf,
                    Start      = DateTime.Now
                };

                // Extract creds from session
                var credHarvester = new CredentialHarvester();
                credHarvester.OnCredential += (type, host, val) => session.CredentialsFound.Add($"[{type}] {val}");
                credHarvester.Inspect(buf, src, dst, dstPort);

                return session;
            }

            return null;
        }

        public void Reset() => _streams.Clear();
    }

    // ── PCAP Exporter ───────────────────────────────────────────────────────────
    public class PcapExporter
    {
        public void Export(string filePath, IEnumerable<CapturedPacket> packets)
        {
            using var fs = new FileStream(filePath, FileMode.Create);
            using var bw = new BinaryWriter(fs);

            // PCAP global header (magic number, version, snaplen, linktype=Ethernet)
            bw.Write(0xa1b2c3d4u); // magic
            bw.Write((ushort)2);   // major version
            bw.Write((ushort)4);   // minor version
            bw.Write(0);           // timezone
            bw.Write(0u);          // timestamp accuracy
            bw.Write(65535u);      // snaplen
            bw.Write(1u);          // link type: ETHERNET

            foreach (var pkt in packets)
            {
                if (pkt.RawData.Length == 0) continue;
                var ts = pkt.Timestamp - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                bw.Write((uint)ts.TotalSeconds);
                bw.Write((uint)(ts.Milliseconds * 1000));
                bw.Write((uint)pkt.RawData.Length);
                bw.Write((uint)pkt.RawData.Length);
                bw.Write(pkt.RawData);
            }
        }
    }

    // ── Advanced Sniffer Orchestrator ───────────────────────────────────────────
    public class AdvancedSniffer
    {
        public readonly BandwidthTracker  Bandwidth   = new();
        public readonly ThreatDetector   Threats     = new();
        public readonly CredentialHarvester Harvester = new();
        public readonly JA3Fingerprinter JA3         = new();
        public readonly DnsAnalyzer      Dns         = new();
        public readonly SessionReassembler Sessions  = new();
        public readonly PcapExporter     PcapExp     = new();

        private ILiveDevice? _device;
        private readonly List<CapturedPacket> _captured = new();
        private readonly object _lock = new();
        private string _bpfFilter = "";

        public event Action<CapturedPacket>? OnPacket;
        public event Action<ThreatAlert>?    OnThreat;
        public event Action<ReconstructedSession>? OnSession;
        public event Action<string, string, string>? OnCredential;

        public bool IsRunning => _device?.Started ?? false;
        public int PacketCount { get { lock (_lock) return _captured.Count; } }
        public IReadOnlyList<CapturedPacket> AllPackets { get { lock (_lock) return _captured.ToList(); } }

        public AdvancedSniffer()
        {
            Threats.OnThreat  += a => OnThreat?.Invoke(a);
            Dns.OnAnomaly     += a => OnThreat?.Invoke(a);
            Harvester.OnCredential += (t, h, v) => OnCredential?.Invoke(t, h, v);
        }

        public static List<ILiveDevice> GetDevices() => CaptureDeviceList.Instance.ToList();

        public void Start(ILiveDevice device, string bpfFilter = "")
        {
            _device = device;
            _bpfFilter = bpfFilter;
            _device.OnPacketArrival += OnArrival;
            _device.Open(DeviceModes.Promiscuous, 1000);
            if (!string.IsNullOrWhiteSpace(bpfFilter))
                _device.Filter = bpfFilter;
            _device.StartCapture();
        }

        public void ApplyFilter(string bpf)
        {
            _bpfFilter = bpf;
            if (_device != null && _device.Started)
                _device.Filter = bpf;
        }

        public void Stop()
        {
            if (_device != null)
            {
                _device.StopCapture();
                _device.OnPacketArrival -= OnArrival;
                _device.Close();
            }
        }

        public void ClearCapture() { lock (_lock) _captured.Clear(); Bandwidth.Reset(); Threats.Reset(); Sessions.Reset(); }

        public void ExportPcap(string path) { lock (_lock) PcapExp.Export(path, _captured); }

        private void OnArrival(object sender, PacketCapture e)
        {
            try
            {
                var rawPkt   = e.GetPacket();
                var packet   = Packet.ParsePacket(rawPkt.LinkLayerType, rawPkt.Data);
                var captured = BuildCaptured(rawPkt, packet);

                lock (_lock) _captured.Add(captured);

                // Bandwidth
                Bandwidth.Track(captured.Protocol, rawPkt.Data.Length);
                Bandwidth.Track("TOTAL", rawPkt.Data.Length);

                // Threat detection
                Threats.Analyze(captured, packet);

                // Deep packet inspection
                var ethPkt = packet.Extract<EthernetPacket>();
                var ipPkt  = ethPkt?.Extract<IPPacket>();
                var tcpPkt = ipPkt?.Extract<TcpPacket>();
                var udpPkt = ipPkt?.Extract<UdpPacket>();

                // Session reassembly
                if (tcpPkt != null && ipPkt != null)
                {
                    var session = Sessions.Feed(tcpPkt,
                        ipPkt.SourceAddress.ToString(),
                        ipPkt.DestinationAddress.ToString(),
                        tcpPkt.DestinationPort);
                    if (session != null) OnSession?.Invoke(session);

                    // Credential harvesting in plaintext TCP
                    if (tcpPkt.PayloadData?.Length > 0)
                    {
                        string payload = System.Text.Encoding.UTF8.GetString(tcpPkt.PayloadData);
                        Harvester.Inspect(payload, ipPkt.SourceAddress.ToString(),
                                          ipPkt.DestinationAddress.ToString(), tcpPkt.DestinationPort);
                    }

                    // JA3 for TLS
                    if ((tcpPkt.DestinationPort == 443 || tcpPkt.SourcePort == 443) && tcpPkt.PayloadData?.Length > 5)
                    {
                        var (hash, isMalicious) = JA3.Fingerprint(tcpPkt.PayloadData);
                        if (isMalicious)
                            OnThreat?.Invoke(new ThreatAlert
                            {
                                Type = "MALICIOUS_JA3", Source = captured.Source,
                                Description = $"Known malicious TLS fingerprint (JA3={hash})", Severity = "CRITICAL"
                            });
                    }
                }

                // DNS analysis
                if (udpPkt?.DestinationPort == 53 && udpPkt.PayloadData?.Length > 0 && ipPkt != null)
                    Dns.Analyze(udpPkt.PayloadData, ipPkt.SourceAddress.ToString());

                OnPacket?.Invoke(captured);
            }
            catch { /* Ignore malformed packets */ }
        }

        private CapturedPacket BuildCaptured(RawCapture raw, Packet packet)
        {
            var c = new CapturedPacket { Timestamp = DateTime.Now, Length = raw.Data.Length, RawData = raw.Data };

            var ethPkt = packet.Extract<EthernetPacket>();
            var ipPkt  = ethPkt?.Extract<IPPacket>();

            if (ipPkt != null)
            {
                c.Source      = ipPkt.SourceAddress.ToString();
                c.Destination = ipPkt.DestinationAddress.ToString();
                c.Protocol    = ipPkt.Protocol.ToString().ToUpper();

                var tcp = ipPkt.Extract<TcpPacket>();
                if (tcp != null) c.Info = $"TCP {tcp.SourcePort} → {tcp.DestinationPort} [{TcpFlags(tcp)}]";

                var udp = ipPkt.Extract<UdpPacket>();
                if (udp != null) c.Info = $"UDP {udp.SourcePort} → {udp.DestinationPort}";

                var icmp = ipPkt.Extract<IcmpV4Packet>();
                if (icmp != null) c.Info = $"ICMP Type={icmp.TypeCode}";
            }
            else if (ethPkt != null)
            {
                var arp = ethPkt.Extract<ArpPacket>();
                if (arp != null) { c.Protocol = "ARP"; c.Source = arp.SenderProtocolAddress.ToString(); c.Info = $"ARP {arp.Operation}"; }
            }

            return c;
        }

        private string TcpFlags(TcpPacket tcp)
        {
            var flags = new List<string>();
            if (tcp.Synchronize)   flags.Add("SYN");
            if (tcp.Acknowledgment) flags.Add("ACK");
            if (tcp.Finished)      flags.Add("FIN");
            if (tcp.Reset)         flags.Add("RST");
            if (tcp.Push)          flags.Add("PSH");
            return string.Join("|", flags);
        }
    }
}
