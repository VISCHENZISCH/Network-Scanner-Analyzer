using NetScanAnalyzer.Models;
using NetScanAnalyzer.Modules;
using NetScanAnalyzer.Utils;
using NetScanAnalyzer.Core;
using NetScanAnalyzer.Reports;

namespace NetScanAnalyzer.UI
{
    public partial class MainForm : Form
    {
        private readonly HostDiscovery       _discovery  = new();
        private readonly PortScanner         _ports      = new();
        private readonly ServiceDetector     _services   = new();
        private readonly SslInspector        _ssl        = new();
        private readonly GeoIpLookup         _geo        = new();
        private readonly CveScanner          _cve        = new();
        private readonly UdpScanner          _udp        = new();
        private readonly OsFingerprinter     _os         = new();
        private readonly RiskReportGenerator _riskGen    = new();
        private readonly JsonExporter        _jsonExp    = new();
        private readonly PdfGenerator        _pdf        = new();
        private readonly ScanHistory         _history    = new();
        private readonly ScanScheduler       _scheduler  = new();
        private ShodanClient _shodan = new("");

        private List<NetworkHost> _hosts = new();
        private CancellationTokenSource? _cts;
        private string _lastRange = "192.168.1.0/24";

        public MainForm()
        {
            InitializeComponent();
        }

        // ── Scan dialog ───────────────────────────────────────────────────────
        private async Task OpenNewScanDialogAsync()
        {
            if (_cts != null) { _cts.Cancel(); return; }

            using var dlg = new Form
            {
                Text = "New Scan", Size = new Size(440, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Theme.White, Font = Theme.FontBase, ForeColor = Theme.TextPrimary
            };

            var lbl = new Label { Text = "IP Range", Location = new Point(20, 18), AutoSize = true, Font = Theme.FontSmBold, ForeColor = Theme.TextSecondary };
            var txt = new TextBox { Location = new Point(20, 38), Size = new Size(382, 30), Text = _lastRange, BorderStyle = BorderStyle.FixedSingle, BackColor = Theme.White, ForeColor = Theme.TextPrimary, Font = Theme.FontBase };

            var btnGo = new Controls.RoundedButton { Text = "Start Scan", Location = new Point(20, 84), Size = new Size(120, 36) };
            btnGo.Click += (_, _) => { dlg.Tag = txt.Text; dlg.DialogResult = DialogResult.OK; };

            var btnCancel = new Controls.RoundedButton { Text = "Cancel", Location = new Point(150, 84), Size = new Size(90, 36), IsOutline = true, ForeColor = Theme.TextPrimary };
            btnCancel.Click += (_, _) => dlg.DialogResult = DialogResult.Cancel;

            dlg.Controls.AddRange(new Control[] { lbl, txt, btnGo, btnCancel });

            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Tag is string range && !string.IsNullOrWhiteSpace(range))
            {
                _lastRange = range;
                await RunScanAsync(range);
            }
        }

        // ── Scan pipeline ─────────────────────────────────────────────────────
        private async Task RunScanAsync(string range)
        {
            var ips = NetworkHelper.ParseIPRange(range);
            if (ips.Count == 0) { MessageBox.Show("Invalid IP range.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            _cts = new CancellationTokenSource();
            _hosts.Clear();
            _scanner.SetHosts(_hosts);
            _sidebar.LoadHosts(_hosts);
            _right.StartScan(range);

            _ring.Visible = true;
            _ring.Value = 0;
            UpdateStatus($"Scanning {range}…", 0, 0);

            try
            {
                // Phase 1 — host discovery
                var progress = new Progress<int>(v => InvokeIfNeeded(() =>
                {
                    _ring.Value = v / 2;
                    _right.UpdateProgress(v / 2);
                }));

                _hosts = await _discovery.ScanRangeAsync(ips, progress);
                InvokeIfNeeded(() => { _scanner.SetHosts(_hosts); _sidebar.LoadHosts(_hosts); });

                // Phase 2 — deep scan
                int done = 0;
                var sem  = new SemaphoreSlim(8);

                await Task.WhenAll(_hosts.Select(async host =>
                {
                    await sem.WaitAsync(_cts.Token);
                    try
                    {
                        if (_cts.Token.IsCancellationRequested) return;

                        var (osStr, ttl) = await _os.FingerprintAsync(host.IPAddress);
                        host.OSFamily = osStr; host.TTL = ttl;

                        var tcp = await _ports.ScanHostPortsAsync(host.IPAddress,
                            new[] { 21, 22, 23, 25, 53, 80, 110, 143, 443, 445, 3306, 3389, 5432, 8080, 8443 });
                        host.OpenPorts = tcp.Where(p => p.Status == PortStatus.Open).ToList();
                        host.OpenPorts.AddRange(await _udp.ScanAsync(host.IPAddress));

                        foreach (var p in host.OpenPorts.Take(3))
                        {
                            p.Banner      = await _services.GrabBannerAsync(host.IPAddress, p.PortNumber);
                            p.ServiceName = _services.FingerprintBanner(p.Banner);
                        }

                        host.GeoInfo = await _geo.LookupAsync(host.IPAddress);

                        if (host.OpenPorts.Any(p => p.PortNumber == 443))
                            host.SslInfo = await _ssl.InspectAsync(host.IPAddress);

                        foreach (var p in host.OpenPorts.Take(3))
                            host.Cves.AddRange(await _cve.LookupByPortAsync(p.PortNumber));

                        host.RiskScore = _riskGen.ComputeRiskScore(host);

                        int pct = 50 + Interlocked.Increment(ref done) * 50 / _hosts.Count;
                        InvokeIfNeeded(() =>
                        {
                            _ring.Value = pct;
                            _right.UpdateProgress(pct);
                            _scanner.SetHosts(_hosts);
                            _sidebar.LoadHosts(_hosts);
                            UpdateStatus($"Scanning {range}…", _hosts.Sum(h => h.OpenPorts.Count), _hosts.Count(h => h.IsOnline));
                        });
                    }
                    finally { sem.Release(); }
                }));

                _history.Save(new ScanSession { IpRange = range, HostsFound = _hosts.Count, TotalOpenPorts = _hosts.Sum(h => h.OpenPorts.Count), Hosts = _hosts });
            }
            catch (OperationCanceledException) { }
            finally
            {
                _cts = null;
                InvokeIfNeeded(() =>
                {
                    _ring.Visible = false;
                    _right.StopScan();
                    UpdateStatus($"Scan complete — {_lastRange}", _hosts.Sum(h => h.OpenPorts.Count), _hosts.Count(h => h.IsOnline));
                });
            }
        }

        // ── Export ────────────────────────────────────────────────────────────
        private void Export(string format)
        {
            if (!_hosts.Any()) { MessageBox.Show("No results to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var filter = format switch { "PDF" => "PDF|*.pdf", "JSON" => "JSON|*.json", "XML" => "XML|*.xml", "HTML" => "HTML|*.html", _ => "CSV|*.csv" };
            var sfd = new SaveFileDialog { Filter = filter, FileName = $"netscan_{DateTime.Now:yyyyMMdd_HHmmss}" };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            try
            {
                switch (format)
                {
                    case "PDF":  _pdf.GenerateReport(sfd.FileName, _hosts); break;
                    case "JSON": _jsonExp.ExportJson(sfd.FileName, _hosts); break;
                    case "XML":  _jsonExp.ExportXml(sfd.FileName, _hosts); break;
                    case "HTML": File.WriteAllText(sfd.FileName, _riskGen.GenerateHtmlReport(_hosts, _lastRange)); break;
                    case "CSV":  _jsonExp.ExportCsv(sfd.FileName, _hosts); break;
                }
                MessageBox.Show("Exported successfully!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void UpdateStatus(string msg, int ports, int live)
        {
            InvokeIfNeeded(() =>
            {
                if (Controls.Find("lblStatusMsg",  true).FirstOrDefault() is Label lm) lm.Text = msg;
                if (Controls.Find("lblStatusPorts", true).FirstOrDefault() is Label lp) lp.Text = $"{ports} open ports";
                if (Controls.Find("lblStatusLive",  true).FirstOrDefault() is Label ll) ll.Text = $"{live} live hosts";
            });
        }

        private void InvokeIfNeeded(Action a) { if (InvokeRequired) Invoke(a); else a(); }
    }
}
