using System.Net;
using System.Net.Sockets;
using NetScanAnalyzer.Models;

namespace NetScanAnalyzer.Modules
{
    public class UdpScanner
    {
        private static readonly Dictionary<int, (string Name, byte[] Probe)> UdpProbes = new()
        {
            { 53,  ("DNS",  new byte[] { 0x00,0x01,0x01,0x00,0x00,0x01,0x00,0x00,0x00,0x00,0x00,0x00,
                                          0x07,0x76,0x65,0x72,0x73,0x69,0x6f,0x6e,0x04,0x62,0x69,0x6e,
                                          0x64,0x00,0x00,0x10,0x00,0x03 }) },
            { 67,  ("DHCP", new byte[] { 0x01,0x01,0x06,0x00,0xde,0xad,0xbe,0xef }) },
            { 123, ("NTP",  new byte[] { 0x1b,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00 }) },
            { 161, ("SNMP", new byte[] { 0x30,0x26,0x02,0x01,0x00,0x04,0x06,0x70,0x75,0x62,0x6c,0x69,
                                          0x63,0xa0,0x19,0x02,0x04,0x63,0x23,0x73,0x1d,0x02,0x01,0x00,
                                          0x02,0x01,0x00,0x30,0x0b,0x30,0x09,0x06,0x05,0x2b,0x06,0x01,
                                          0x02,0x01,0x05,0x00 }) },
            { 69,  ("TFTP", new byte[] { 0x00,0x01,0x74,0x65,0x73,0x74,0x00,0x6f,0x63,0x74,0x65,0x74,0x00 }) },
        };

        public async Task<List<NetworkPort>> ScanAsync(string ip, int timeout = 1500)
        {
            var results = new List<NetworkPort>();
            var tasks = UdpProbes.Select(async kv =>
            {
                var port = kv.Key;
                var (name, probe) = kv.Value;
                try
                {
                    using var udp = new UdpClient();
                    udp.Client.ReceiveTimeout = timeout;
                    await udp.SendAsync(probe, probe.Length, ip, port);
                    var result = await udp.ReceiveAsync();
                    if (result.Buffer.Length > 0)
                        lock (results)
                            results.Add(new NetworkPort { PortNumber = port, Protocol = "UDP", ServiceName = name, Status = PortStatus.Open });
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // ICMP Port Unreachable = definitely closed
                }
                catch { /* filtered or no response */ }
            });

            await Task.WhenAll(tasks);
            return results;
        }
    }
}
