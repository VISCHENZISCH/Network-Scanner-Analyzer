using NetScanAnalyzer.Models;
using NetScanAnalyzer.UI.Controls;
using System.Drawing.Drawing2D;

namespace NetScanAnalyzer.UI.Panels
{
    public class SnifferPanel : UserControl
    {
        private DataGridView _dgvPackets = null!, _dgvThreats = null!, _dgvSessions = null!, _dgvCreds = null!;
        private RichTextBox _rtbHex = null!;
        private Panel _pnlBandwidth = null!;
        private Panel _tabBar = null!;
        private Panel _currentPage = null!;
        private ComboBox _cmbDevice = null!, _cmbPreset = null!;
        private TextBox _txtBpf = null!;
        private IconBtn _btnStart = null!, _btnStop = null!, _btnFilter = null!, _btnClear = null!, _btnPcap = null!;
        private Label _lblStats = null!;
        private System.Windows.Forms.Timer _tmr = null!;
        private Core.Sniffer.AdvancedSniffer? _sniffer;
        private readonly object _lk = new();
        private readonly List<CapturedPacket> _pkts = new();
        private readonly List<ThreatAlert> _threats = new();
        private readonly List<ReconstructedSession> _sessions = new();
        private readonly List<(string T, string H, string V)> _creds = new();
        private readonly Dictionary<string, double> _bwData = new();
        private readonly double[] _bwHistory = new double[60];
        private int _bwIdx;
        private int _tabIdx = 0;
        private readonly Panel[] _pages = new Panel[5];
        private readonly IconBtn[] _tabBtns = new IconBtn[5];
        private readonly Panel _pnlBwInner;

        public SnifferPanel()
        {
            _pnlBwInner = new Panel();
            Dock = DockStyle.Fill;
            BackColor = Theme.Surface2;
            DoubleBuffered = true;
            InitUI();
        }

        private void InitUI()
        {
            // ── Toolbar — row 1: controls ─────────────────────────────────
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 82, BackColor = Theme.White };
            toolbar.Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, toolbar.Height-1, toolbar.Width, toolbar.Height-1);

            // Device picker — wider
            _cmbDevice = StyledCombo(new Size(280, 38));
            _cmbDevice.Location = new Point(20, 22);

            // BPF filter — same height
            _txtBpf = new TextBox { Location = new Point(308, 22), Size = new Size(165, 38), PlaceholderText = "BPF filter…", BorderStyle = BorderStyle.FixedSingle, BackColor = Theme.White, ForeColor = Theme.TextPrimary, Font = Theme.FontBase };

            // Preset — same height
            _cmbPreset = StyledCombo(new Size(140, 38));
            _cmbPreset.Location = new Point(481, 22);
            _cmbPreset.Items.AddRange(new object[] { "No filter", "HTTP", "DNS", "ARP", "TCP", "ICMP", "TLS/HTTPS" });
            _cmbPreset.SelectedIndex = 0;
            _cmbPreset.SelectedIndexChanged += (_, _) => _txtBpf.Text = _cmbPreset.SelectedIndex switch {
                1 => "tcp port 80 or tcp port 8080", 2 => "udp port 53", 3 => "arp",
                4 => "tcp", 5 => "icmp", 6 => "tcp port 443", _ => "" };

            // Buttons — h=38, all at y=22
            int bx = 635;
            _btnStart  = IBtn("Start",  Icons.Play,     Theme.DotActive,     bx, 22, 110);  bx += 118;
            _btnStop   = IBtn("Stop",   Icons.Stop,     Theme.DotInactive,   bx, 22, 105);  bx += 113;
            var sep = new Panel { Location = new Point(bx + 6, 28), Size = new Size(1, 26), BackColor = Theme.Border }; bx += 20;
            _btnFilter = IBtn("Filter", Icons.Filter,   Theme.Accent,        bx, 22, 105, true);  bx += 113;
            _btnClear  = IBtn("Clear",  Icons.Trash,    Theme.TextSecondary, bx, 22, 105, true);  bx += 113;
            _btnPcap   = IBtn("PCAP",   Icons.Download, Theme.Accent,        bx, 22, 105, true);

            // Stats row
            _lblStats = new Label { Location = new Point(20, 64), Size = new Size(800, 16), Font = Theme.FontSm, ForeColor = Theme.TextSecondary, BackColor = Theme.White };

            toolbar.Controls.AddRange(new Control[] { _cmbDevice, _txtBpf, _cmbPreset, _btnStart, _btnStop, sep, _btnFilter, _btnClear, _btnPcap, _lblStats });

            // ── Tab bar (WPS-style pill tabs) ─────────────────────────────
            _tabBar = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Theme.White };
            _tabBar.Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, _tabBar.Height-1, _tabBar.Width, _tabBar.Height-1);

            (string Lbl, Action<Graphics, RectangleF, Color> Ic, int W)[] tabs = {
                ("Packets",     Icons.Wifi,             125),
                ("Threats",     Icons.StatHighRisk,     120),
                ("Sessions",    Icons.NavCapture,       120),
                ("Credentials", Icons.Filter,           140),
                ("Bandwidth",   Icons.StatHostsScanned, 135) };
            int tx = 20;
            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                string lbl = tabs[i].Lbl;
                var ic = tabs[i].Ic;
                var btn = new IconBtn(lbl, (g, r, c) => ic(g, r, c), Theme.Accent, Theme.AccentLight, true)
                    { Location = new Point(tx, 13), Size = new Size(tabs[i].W, 34) };
                btn.Click += (_, _) => SwitchTab(idx);
                _tabBtns[i] = btn;
                _tabBar.Controls.Add(btn);
                tx += tabs[i].W + 14;
            }

            // ── Pages ─────────────────────────────────────────────────────
            _pages[0] = BuildPacketsPage();
            _pages[1] = BuildThreatsPage();
            _pages[2] = BuildSessionsPage();
            _pages[3] = BuildCredsPage();
            _pages[4] = BuildBandwidthPage();

            var pageHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.White };
            foreach (var p in _pages) { p.Dock = DockStyle.Fill; p.Visible = false; pageHost.Controls.Add(p); }

            // ── Stats bar ─────────────────────────────────────────────────
            var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 32, BackColor = Theme.Surface2 };
            statusBar.Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, 0, statusBar.Width, 0);
            var lblStatus = new Label { Dock = DockStyle.Fill, Text = "Capture idle", Font = Theme.FontSm, ForeColor = Theme.TextSecondary, BackColor = Theme.Surface2, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16,0,0,0), Name = "lblCapStatus" };
            statusBar.Controls.Add(lblStatus);

            Controls.Add(pageHost);
            Controls.Add(_tabBar);
            Controls.Add(toolbar);
            Controls.Add(statusBar);

            // Timer
            _tmr = new System.Windows.Forms.Timer { Interval = 1000 };
            _tmr.Tick += (_, _) => UpdateStats();
            _tmr.Start();

            // Events
            _btnStart.Click  += BtnStart_Click;
            _btnStop.Click   += BtnStop_Click;
            _btnFilter.Click += (_, _) => _sniffer?.ApplyFilter(_txtBpf.Text);
            _btnClear.Click  += BtnClear_Click;
            _btnPcap.Click   += BtnPcap_Click;
            _btnStop.Enabled = false;

            // Defer initial tab switch until the control handle exists,
            // otherwise SplitContainer triggers a GDI+ error during layout.
            HandleCreated += (_, _) => SwitchTab(0);

            // Load devices
            try {
                foreach (var d in Core.Sniffer.AdvancedSniffer.GetDevices())
                    _cmbDevice.Items.Add(d.Description ?? d.Name);
                if (_cmbDevice.Items.Count > 0) _cmbDevice.SelectedIndex = 0;
            } catch { _cmbDevice.Items.Add("Npcap not installed"); }
        }

        // ── Pages ─────────────────────────────────────────────────────────
        private Panel BuildPacketsPage()
        {
            var p = new Panel { BackColor = Theme.White };
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 280, BackColor = Theme.Border };
            _dgvPackets = MakeGrid();
            foreach (var (n, h) in new[] { ("Time","Time"), ("Source","Source"), ("Dest","Destination"), ("Proto","Protocol"), ("Len","Len"), ("Info","Info") })
                _dgvPackets.Columns.Add(n, h);
            _dgvPackets.Columns["Info"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _dgvPackets.SelectionChanged += DgvPackets_SelectionChanged;

            // Hex pane
            var hexBar = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Theme.Surface2, Padding = new Padding(12, 8, 0, 0) };
            hexBar.Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, 0, hexBar.Width, 0);
            hexBar.Controls.Add(new Label { Text = "Packet Hex View", Font = Theme.FontSmBold, ForeColor = Theme.TextSecondary, BackColor = Theme.Surface2, AutoSize = true });

            _rtbHex = new RichTextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(252, 252, 253), ForeColor = Theme.TextPrimary, Font = new Font("Consolas", 9f), BorderStyle = BorderStyle.None, ReadOnly = true };

            split.Panel1.Controls.Add(_dgvPackets);
            split.Panel2.Controls.Add(_rtbHex);
            split.Panel2.Controls.Add(hexBar);
            p.Controls.Add(split);
            return p;
        }

        private Panel BuildThreatsPage()
        {
            var p = new Panel { BackColor = Theme.White };
            _dgvThreats = MakeGrid();
            foreach (var (n, h) in new[] { ("Time","Time"), ("Severity","Severity"), ("Type","Type"), ("Source","Source"), ("Desc","Description") })
                _dgvThreats.Columns.Add(n, h);
            _dgvThreats.Columns["Desc"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _dgvThreats.CellPainting += ThreatCell_Paint;

            var banner = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Theme.HighBg, Padding = new Padding(14, 0, 0, 0) };
            var bannerLbl = new Label { Text = "⚠  Threat detection is real-time. Review each alert carefully.", Dock = DockStyle.Fill, Font = Theme.FontSmBold, ForeColor = Theme.HighFg, BackColor = Theme.HighBg, TextAlign = ContentAlignment.MiddleLeft };
            banner.Controls.Add(bannerLbl);
            p.Controls.Add(_dgvThreats);
            p.Controls.Add(banner);
            return p;
        }

        private Panel BuildSessionsPage()
        {
            var p = new Panel { BackColor = Theme.White };
            _dgvSessions = MakeGrid();
            foreach (var (n, h) in new[] { ("Proto","Protocol"), ("Client","Client"), ("Server","Server"), ("Port","Port"), ("Creds","Credentials") })
                _dgvSessions.Columns.Add(n, h);
            _dgvSessions.Columns["Creds"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            p.Controls.Add(_dgvSessions);
            return p;
        }

        private Panel BuildCredsPage()
        {
            var p = new Panel { BackColor = Theme.White };
            _dgvCreds = MakeGrid();
            foreach (var (n, h) in new[] { ("Type","Type"), ("Host","Host"), ("Value","Captured Credential") })
                _dgvCreds.Columns.Add(n, h);
            _dgvCreds.Columns["Value"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            var warn = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Theme.MedBg, Padding = new Padding(14, 0, 0, 0) };
            warn.Controls.Add(new Label { Text = "⚠  For authorized penetration testing only. Handle with care.", Dock = DockStyle.Fill, Font = Theme.FontSmBold, ForeColor = Theme.MedFg, BackColor = Theme.MedBg, TextAlign = ContentAlignment.MiddleLeft });
            p.Controls.Add(_dgvCreds);
            p.Controls.Add(warn);
            return p;
        }

        private Panel BuildBandwidthPage()
        {
            var p = new Panel { BackColor = Theme.White };
            _pnlBandwidth = new Panel { Dock = DockStyle.Fill, BackColor = Theme.White };
            _pnlBandwidth.Paint += BandwidthPaint;
            p.Controls.Add(_pnlBandwidth);
            return p;
        }

        // ── Tab switch ────────────────────────────────────────────────────
        private void SwitchTab(int idx)
        {
            _tabIdx = idx;
            for (int i = 0; i < _pages.Length; i++) _pages[i].Visible = i == idx;
            for (int i = 0; i < _tabBtns.Length; i++)
            {
                // Highlight selected tab — swap bg color trick via Tag
                _tabBtns[i].Tag = i == idx;
                _tabBtns[i].Invalidate();
            }
        }

        // ── Bandwidth chart (light, WPS-style) ────────────────────────────
        private void BandwidthPaint(object? s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.White);

            var protos = new[] {
                ("TCP",  Theme.Accent),
                ("UDP",  Theme.DotActive),
                ("ICMP", Theme.DotWarning),
                ("ARP",  Theme.DotInactive)
            };

            int w = _pnlBandwidth.Width, h = _pnlBandwidth.Height;

            // Title
            using var tf = Theme.FontLg;
            g.DrawString("Live Bandwidth", tf, new SolidBrush(Theme.TextPrimary), 20, 16);

            double total = _bwData.TryGetValue("TOTAL", out var tv) ? tv : 1;
            string rate = $"{total/1024:F1} KB/s total";
            using var sf2 = Theme.FontSm;
            g.DrawString(rate, sf2, new SolidBrush(Theme.TextSecondary), w - 140, 20);

            // Bars
            int y = 58;
            double maxBps = Math.Max(1, protos.Sum(p2 => _bwData.TryGetValue(p2.Item1, out var v) ? v : 0));
            foreach (var (proto, color) in protos)
            {
                double bps = _bwData.TryGetValue(proto, out var val) ? val : 0;
                float barMax = w - 180;
                float barW = (float)(bps / maxBps * barMax);

                // Track
                using var trackPath = RR(new Rectangle(100, y, (int)barMax, 22), 11);
                g.FillPath(new SolidBrush(Theme.Surface2), trackPath);

                // Fill
                if (barW > 2)
                {
                    using var fillPath = RR(new Rectangle(100, y, (int)barW, 22), 11);
                    g.FillPath(new SolidBrush(Color.FromArgb(200, color)), fillPath);
                }

                using var pf = Theme.FontSmBold;
                g.DrawString(proto, pf, new SolidBrush(Theme.TextSecondary), 14, y + 3);
                g.DrawString($"{bps/1024:F1} KB/s", pf, new SolidBrush(color), w - 75, y + 3);
                y += 36;
            }

            // Sparkline history
            if (_bwHistory.Any(v => v > 0))
            {
                int sparkY = y + 20, sparkH = Math.Min(80, h - sparkY - 30);
                if (sparkH > 20)
                {
                    g.DrawString("History (60s)", Theme.FontSmBold, new SolidBrush(Theme.TextSecondary), 14, sparkY - 18);
                    double hMax = Math.Max(1, _bwHistory.Max());
                    var pts = new List<PointF>();
                    for (int i = 0; i < 60; i++)
                    {
                        float px = 14 + i * (w - 28) / 60f;
                        int hi = (_bwIdx + i) % 60;
                        float py = sparkY + sparkH - (float)(_bwHistory[hi] / hMax * sparkH);
                        pts.Add(new PointF(px, py));
                    }
                    if (pts.Count > 1)
                        g.DrawLines(new Pen(Theme.Accent, 2f), pts.ToArray());
                }
            }
        }

        // ── Grid painting helpers ─────────────────────────────────────────
        private void ThreatCell_Paint(object? s, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;
            e.PaintBackground(e.ClipBounds, true);
            string sev = e.Value?.ToString() ?? "";
            var (bg, fg) = Theme.BadgeColors(sev);
            var g2 = e.Graphics!;
            g2.SmoothingMode = SmoothingMode.AntiAlias;
            var br = new RectangleF(e.CellBounds.X + 6, e.CellBounds.Y + 6, 68, 20);
            using var path = RR(Rectangle.Round(br), 10);
            g2.FillPath(new SolidBrush(bg), path);
            using var sff = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g2.DrawString(sev, Theme.FontSmBold, new SolidBrush(fg), br, sff);
            e.Handled = true;
        }

        private void ColorPacketRow(DataGridViewRow row, string proto)
        {
            row.DefaultCellStyle.BackColor = proto switch {
                "TCP"  => Color.FromArgb(239, 246, 255),
                "UDP"  => Color.FromArgb(236, 253, 245),
                "ICMP" => Color.FromArgb(255, 251, 235),
                "ARP"  => Color.FromArgb(255, 241, 242),
                _      => Theme.White
            };
            row.DefaultCellStyle.ForeColor = Theme.TextPrimary;
        }

        // ── Scan events ───────────────────────────────────────────────────
        private void BtnStart_Click(object? s, EventArgs e)
        {
            if (_cmbDevice.SelectedIndex < 0) return;
            try {
                var devices = Core.Sniffer.AdvancedSniffer.GetDevices();
                if (_cmbDevice.SelectedIndex >= devices.Count) return;
                _sniffer = new Core.Sniffer.AdvancedSniffer();
                _sniffer.OnPacket     += AddPacket;
                _sniffer.OnThreat     += AddThreat;
                _sniffer.OnSession    += AddSession;
                _sniffer.OnCredential += AddCredential;
                _sniffer.Start(devices[_cmbDevice.SelectedIndex], _txtBpf.Text);
                _btnStart.Enabled = false; _btnStop.Enabled = true;
                SetStatus("Capturing…", Theme.DotActive);
            } catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Sniffer", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnStop_Click(object? s, EventArgs e) {
            _sniffer?.Stop();
            _btnStart.Enabled = true; _btnStop.Enabled = false;
            SetStatus("Capture stopped", Theme.TextSecondary); }

        private void BtnClear_Click(object? s, EventArgs e) {
            _sniffer?.ClearCapture();
            lock (_lk) { _pkts.Clear(); _threats.Clear(); _sessions.Clear(); _creds.Clear(); }
            if (InvokeRequired) Invoke(ClearGrids); else ClearGrids(); }

        private void BtnPcap_Click(object? s, EventArgs e) {
            if (_sniffer == null) return;
            var sfd = new SaveFileDialog { Filter = "PCAP|*.pcap", FileName = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.pcap" };
            if (sfd.ShowDialog() == DialogResult.OK) _sniffer.ExportPcap(sfd.FileName); }

        private void ClearGrids() { _dgvPackets.Rows.Clear(); _dgvThreats.Rows.Clear(); _dgvSessions.Rows.Clear(); _dgvCreds.Rows.Clear(); }

        private void AddPacket(CapturedPacket pkt) {
            lock (_lk) _pkts.Add(pkt);
            if (!IsHandleCreated) return;
            BeginInvoke(() => {
                if (_dgvPackets.Rows.Count > 5000) _dgvPackets.Rows.RemoveAt(0);
                int idx = _dgvPackets.Rows.Add(pkt.Timestamp.ToString("HH:mm:ss.fff"), pkt.Source, pkt.Destination, pkt.Protocol, pkt.Length, pkt.Info);
                ColorPacketRow(_dgvPackets.Rows[idx], pkt.Protocol); }); }

        private void AddThreat(ThreatAlert alert) {
            lock (_lk) _threats.Add(alert);
            if (!IsHandleCreated) return;
            BeginInvoke(() => { _dgvThreats.Rows.Add(alert.Timestamp.ToString("HH:mm:ss"), alert.Severity, alert.Type, alert.Source, alert.Description);
                UpdateTabBadge(1, _threats.Count); }); }

        private void AddSession(ReconstructedSession sess) {
            lock (_lk) _sessions.Add(sess);
            if (!IsHandleCreated) return;
            BeginInvoke(() => _dgvSessions.Rows.Add(sess.Protocol, sess.ClientIp, sess.ServerIp, sess.ServerPort, sess.CredentialsFound.Count > 0 ? "✓ YES" : "")); }

        private void AddCredential(string type, string host, string val) {
            lock (_lk) _creds.Add((type, host, val));
            if (!IsHandleCreated) return;
            BeginInvoke(() => { _dgvCreds.Rows.Add(type, host, val); UpdateTabBadge(3, _creds.Count); }); }

        private void DgvPackets_SelectionChanged(object? s, EventArgs e) {
            if (_dgvPackets.SelectedRows.Count == 0) return;
            int idx = _dgvPackets.SelectedRows[0].Index;
            lock (_lk) {
                if (idx >= _pkts.Count) return;
                var pkt = _pkts[idx];
                var sb = new System.Text.StringBuilder();
                var data = pkt.RawData;
                for (int i = 0; i < Math.Min(data.Length, 512); i += 16) {
                    sb.Append($"{i:X4}  ");
                    for (int j = i; j < Math.Min(i+16, data.Length); j++) sb.Append($"{data[j]:X2} ");
                    sb.Append("  ");
                    for (int j = i; j < Math.Min(i+16, data.Length); j++) sb.Append(data[j] >= 32 && data[j] < 127 ? (char)data[j] : '.');
                    sb.AppendLine();
                }
                _rtbHex.Text = sb.ToString(); } }

        private void UpdateStats() {
            if (_sniffer == null) return;
            double bps = _sniffer.Bandwidth.GetBytesPerSec("TOTAL");
            _bwHistory[_bwIdx++ % 60] = bps;
            _lblStats.Text = $"Packets: {_sniffer.PacketCount:N0}  │  Threats: {_threats.Count}  │  Sessions: {_sessions.Count}  │  {bps/1024:F1} KB/s";
            lock (_bwData) { var all = _sniffer.Bandwidth.GetAllBps(); foreach (var kv in all) _bwData[kv.Key] = kv.Value; }
            _pnlBandwidth.Invalidate(); }

        private void UpdateTabBadge(int idx, int count) {
            if (idx < _tabBtns.Length) { _tabBtns[idx].Text = $"{(new[]{"Packets","Threats","Sessions","Credentials","Bandwidth"})[idx]} ({count})"; _tabBtns[idx].Invalidate(); } }

        private void SetStatus(string msg, Color c) {
            if (Controls.Find("lblCapStatus", true).FirstOrDefault() is Label l) { l.Text = msg; l.ForeColor = c; } }

        // ── Factories ─────────────────────────────────────────────────────
        private DataGridView MakeGrid()
        {
            return new DataGridView {
                Dock = DockStyle.Fill, BackgroundColor = Theme.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, EnableHeadersVisualStyles = false,
                GridColor = Theme.Border, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                RowTemplate = { Height = 30 },
                Font = new Font("Consolas", 8.5f),
                DefaultCellStyle = new DataGridViewCellStyle {
                    BackColor = Theme.White, ForeColor = Theme.TextPrimary,
                    SelectionBackColor = Theme.AccentLight, SelectionForeColor = Theme.TextPrimary,
                    Padding = new Padding(6, 0, 6, 0) },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.RowAlt, ForeColor = Theme.TextPrimary },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                    BackColor = Theme.Surface2, ForeColor = Theme.HeaderText,
                    SelectionBackColor = Theme.Surface2, Font = Theme.FontSmBold,
                    Padding = new Padding(6, 0, 6, 0) },
                ColumnHeadersHeight = 36, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing };
        }

        private ComboBox StyledCombo(Size sz) => new() { Size = sz, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.White, ForeColor = Theme.TextPrimary, Font = Theme.FontBase };

        private IconBtn IBtn(string lbl, Action<Graphics, RectangleF, Color> icon, Color c, int x, int y, int w, bool outline = false)
        {
            var b = new IconBtn(lbl, icon, c, outline ? Theme.White : c, outline) { Location = new Point(x, y), Width = w };
            if (!outline) { /* solid bg already set */ }
            return b;
        }

        private static GraphicsPath RR(Rectangle r, int rad = 6) {
            float d = rad*2f; var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right-d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right-d, r.Bottom-d, d, d, 0, 90); p.AddArc(r.X, r.Bottom-d, d, d, 90, 90);
            p.CloseFigure(); return p; }
    }
}
