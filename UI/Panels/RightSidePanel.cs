using NetScanAnalyzer.Models;
using NetScanAnalyzer.UI.Controls;
using System.Drawing.Drawing2D;

namespace NetScanAnalyzer.UI.Panels
{
    public class RightSidePanel : UserControl
    {
        private CircularProgressRing _ring = null!;
        private Label _lblProgress = null!, _lblIp = null!, _lblElapsed = null!;
        private RoundedPanel _pnlDetails = null!;
        private FlowLayoutPanel _flowCves = null!;
        private Panel _chartPanel = null!;
        private System.Windows.Forms.Timer _timer = null!;
        private DateTime _scanStart;
        private bool _scanning;

        public RightSidePanel()
        {
            Dock = DockStyle.Right;
            Width = 300;
            BackColor = Theme.Surface2;
            DoubleBuffered = true;
            Padding = new Padding(16, 0, 16, 0);
            Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, 0, 0, Height);
            InitUI();
        }

        private void InitUI()
        {
            // Outer flow
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Theme.Surface2,
                Padding = new Padding(0, 12, 0, 12),
            };

            flow.Controls.Add(BuildScanCard());
            flow.Controls.Add(Spacer(12));
            flow.Controls.Add(BuildDetailsCard());
            flow.Controls.Add(Spacer(12));
            flow.Controls.Add(BuildCveCard());
            flow.Controls.Add(Spacer(12));
            flow.Controls.Add(BuildChartCard());

            Controls.Add(flow);

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (_, _) =>
            {
                if (_scanning) _lblElapsed.Text = $"{(DateTime.Now - _scanStart):mm\\:ss} elapsed";
            };
        }

        // ── Scan Progress Card ────────────────────────────────────────────────
        private RoundedPanel BuildScanCard()
        {
            var card = Card(138);
            var title = HeaderLabel("Scan Progress", 16, 14);
            var divider = new Panel { Location = new Point(0, 38), Size = new Size(268, 1), BackColor = Theme.Border };

            _ring = new CircularProgressRing
            {
                Location = new Point(16, 52),
                Size = new Size(72, 72),
                RingThickness = 6f,
                BackColor = Theme.White
            };

            _lblProgress = new Label { Text = "Idle", Location = new Point(102, 54), Width = 152, Font = Theme.FontBold, ForeColor = Theme.TextPrimary, BackColor = Theme.White };
            _lblIp       = new Label { Text = "—",   Location = new Point(102, 76), Width = 152, Font = Theme.FontSm,   ForeColor = Theme.TextSecondary, BackColor = Theme.White };
            _lblElapsed  = new Label { Text = "",     Location = new Point(102, 96), Width = 152, Font = Theme.FontSm,   ForeColor = Theme.TextMuted, BackColor = Theme.White };

            var btnPause = new RoundedButton
            {
                Text = "Pause",
                Location = new Point(102, 112),
                Size = new Size(76, 28),
                NormalColor = Theme.White,
                ForeColor = Theme.TextPrimary,
                IsOutline = true,
                CornerRadius = 6,
                Font = Theme.FontSmBold,
            };
            btnPause.Click += (_, _) =>
            {
                _scanning = !_scanning;
                btnPause.Text = _scanning ? "Pause" : "Resume";
            };

            card.Controls.AddRange(new Control[] { title, divider, _ring, _lblProgress, _lblIp, _lblElapsed, btnPause });
            return card;
        }

        // ── Host Details Card ─────────────────────────────────────────────────
        private RoundedPanel BuildDetailsCard()
        {
            _pnlDetails = Card(220);

            var title = HeaderLabel("Host Details", 16, 14);
            var divider = new Panel { Location = new Point(0, 38), Size = new Size(268, 1), BackColor = Theme.Border };

            var noSel = new Label
            {
                Text = "Select a host to view details",
                Location = new Point(16, 60), Width = 236, Height = 60,
                Font = Theme.FontBase, ForeColor = Theme.TextMuted,
                BackColor = Theme.White, TextAlign = ContentAlignment.MiddleCenter,
                Name = "lblNoSel"
            };
            _pnlDetails.Controls.AddRange(new Control[] { title, divider, noSel });
            return _pnlDetails;
        }

        // ── CVE Card ──────────────────────────────────────────────────────────
        private RoundedPanel BuildCveCard()
        {
            var card = Card(180);
            var title = HeaderLabel("Vulnerabilities (CVE)", 16, 14);
            var divider = new Panel { Location = new Point(0, 38), Size = new Size(268, 1), BackColor = Theme.Border };

            _flowCves = new FlowLayoutPanel
            {
                Location = new Point(0, 44),
                Size = new Size(268, 130),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Theme.White,
                AutoScroll = false,
                Padding = new Padding(12, 0, 12, 0),
            };

            var empty = new Label { Text = "No CVEs found", Font = Theme.FontSm, ForeColor = Theme.TextMuted, BackColor = Theme.White, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
            _flowCves.Controls.Add(empty);

            card.Controls.AddRange(new Control[] { title, divider, _flowCves });
            return card;
        }

        // ── Port Category Chart ───────────────────────────────────────────────
        private RoundedPanel BuildChartCard()
        {
            var card = Card(180);
            var title = HeaderLabel("Ports by Category", 16, 14);
            var divider = new Panel { Location = new Point(0, 38), Size = new Size(268, 1), BackColor = Theme.Border };

            _chartPanel = new Panel { Location = new Point(0, 44), Size = new Size(268, 130), BackColor = Theme.White };
            _chartPanel.Paint += ChartPanel_Paint;

            card.Controls.AddRange(new Control[] { title, divider, _chartPanel });
            return card;
        }

        private Dictionary<string, int> _chartData = new();
        private void ChartPanel_Paint(object? s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.White);

            if (_chartData.Count == 0)
            {
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("No data yet", Theme.FontSm, new SolidBrush(Theme.TextMuted), _chartPanel.ClientRectangle, sf);
                return;
            }

            int max = _chartData.Values.DefaultIfEmpty(0).Max();
            if (max == 0) max = 1;

            float pad = 14f, chartH = _chartPanel.Height - 36f;
            float barW = (_chartPanel.Width - pad * 2 - (_chartData.Count - 1) * 6f) / _chartData.Count;
            float x = pad;

            var barColors = new[] { Theme.Accent, Color.FromArgb(0x10, 0xB9, 0x81), Color.FromArgb(0xF5, 0x9E, 0x0B), Color.FromArgb(0xEF, 0x44, 0x44), Color.FromArgb(0x8B, 0x5C, 0xF6), Theme.TextMuted };
            int ci = 0;

            foreach (var kv in _chartData)
            {
                float barH = Math.Max(2f, chartH * kv.Value / max);
                float by = pad + chartH - barH;

                // Bar with rounded top
                using var path = BarPath(x, by, barW, barH, 4);
                g.FillPath(new SolidBrush(barColors[ci % barColors.Length]), path);

                // Value label
                if (kv.Value > 0)
                {
                    using var vf = Theme.FontSmBold;
                    string val = kv.Value.ToString();
                    var vsz = g.MeasureString(val, vf);
                    g.DrawString(val, vf, new SolidBrush(Theme.TextPrimary), x + (barW - vsz.Width) / 2, by - vsz.Height - 2);
                }

                // X label
                using var lf = Theme.FontSm;
                var lsz = g.MeasureString(kv.Key, lf);
                g.DrawString(kv.Key, lf, new SolidBrush(Theme.TextSecondary), x + (barW - lsz.Width) / 2, pad + chartH + 4);

                x += barW + 6;
                ci++;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void StartScan(string range)
        {
            _scanning = true;
            _scanStart = DateTime.Now;
            _lblProgress.Text = "Scanning...";
            _lblIp.Text = range;
            _lblElapsed.Text = "00:00 elapsed";
            _ring.Value = 0;
            _timer.Start();
        }

        public void UpdateProgress(int pct)
        {
            if (InvokeRequired) { Invoke(() => UpdateProgress(pct)); return; }
            _ring.Value = pct;
        }

        public void StopScan()
        {
            if (InvokeRequired) { Invoke(StopScan); return; }
            _scanning = false;
            _timer.Stop();
            _lblProgress.Text = "Complete";
            _ring.Value = 100;
        }

        public void ShowHostDetails(NetworkHost host)
        {
            if (InvokeRequired) { Invoke(() => ShowHostDetails(host)); return; }
            _pnlDetails.SuspendLayout();

            // Remove old dynamic controls (keep: title, divider, noSel stub)
            var toRemove = _pnlDetails.Controls.Cast<Control>().Where(c => c.Name?.StartsWith("dyn_") == true).ToList();
            toRemove.ForEach(c => _pnlDetails.Controls.Remove(c));
            var noSel = _pnlDetails.Controls["lblNoSel"];
            if (noSel != null) noSel.Visible = false;

            int y = 48;

            // Host header row
            var dotColor = host.IsOnline ? Theme.DotActive : Theme.DotInactive;
            var dot = new StatusDot { Location = new Point(16, y + 4), DotColor = dotColor, Name = "dyn_dot" };

            var ipLabel = new Label { Text = host.IPAddress, Location = new Point(30, y), AutoSize = true, Font = Theme.FontLg, ForeColor = Theme.TextPrimary, BackColor = Theme.White, Name = "dyn_ip" };

            var badge = new BadgeLabel { Risk = host.RiskLabel, Location = new Point(200, y + 2), Size = new Size(60, 22), Name = "dyn_badge" };

            y += 30;
            var osLabel = new Label { Text = host.OSFamily, Location = new Point(16, y), AutoSize = true, Font = Theme.FontSm, ForeColor = Theme.TextSecondary, BackColor = Theme.White, Name = "dyn_os" };
            y += 20;

            var sep = new Panel { Location = new Point(0, y), Size = new Size(268, 1), BackColor = Theme.Border, Name = "dyn_sep" };
            y += 12;

            _pnlDetails.Controls.AddRange(new Control[] { dot, ipLabel, badge, osLabel, sep });

            // Field rows
            var fields = new (string K, string V)[]
            {
                ("MAC",        host.MACAddress),
                ("Hostname",   host.Hostname),
                ("Country",    host.GeoInfo?.Country ?? "—"),
                ("Open Ports", host.OpenPorts.Count.ToString()),
                ("Last Seen",  host.LastScanned.ToString("MMM d, yyyy HH:mm")),
            };

            foreach (var (k, v) in fields)
            {
                var kl = new Label { Text = k, Location = new Point(16, y), Size = new Size(90, 18), Font = Theme.FontSm,   ForeColor = Theme.TextSecondary, BackColor = Theme.White, Name = $"dyn_k_{k}" };
                var vl = new Label { Text = v, Location = new Point(112, y), Size = new Size(152, 18), Font = Theme.FontSmBold, ForeColor = Theme.TextPrimary,   BackColor = Theme.White, Name = $"dyn_v_{k}" };
                _pnlDetails.Controls.AddRange(new Control[] { kl, vl });
                y += 22;
            }

            // View Full Details button
            y += 6;
            var btnFull = new RoundedButton
            {
                Text = "View Full Details",
                Location = new Point(16, y), Size = new Size(236, 32),
                NormalColor = Theme.Accent, IsOutline = false,
                Name = "dyn_btn"
            };
            _pnlDetails.Controls.Add(btnFull);
            _pnlDetails.Height = y + 44;

            _pnlDetails.ResumeLayout();

            // CVEs
            ShowCves(host.Cves);

            // Chart
            _chartData = new Dictionary<string, int>
            {
                { "Web",    host.OpenPorts.Count(p => p.PortNumber is 80 or 443 or 8080 or 8443) },
                { "SSH",    host.OpenPorts.Count(p => p.PortNumber is 22 or 23) },
                { "DB",     host.OpenPorts.Count(p => p.PortNumber is 3306 or 5432 or 1433 or 27017) },
                { "Mail",   host.OpenPorts.Count(p => p.PortNumber is 25 or 110 or 143 or 465 or 587) },
                { "File",   host.OpenPorts.Count(p => p.PortNumber is 21 or 445 or 139 or 2049) },
                { "Other",  Math.Max(0, host.OpenPorts.Count - host.OpenPorts.Count(p => p.PortNumber is 80 or 443 or 8080 or 8443 or 22 or 23 or 3306 or 5432 or 1433 or 27017 or 25 or 110 or 143 or 465 or 587 or 21 or 445 or 139 or 2049)) }
            };
            _chartPanel.Invalidate();
        }

        private void ShowCves(List<CveEntry> cves)
        {
            _flowCves.Controls.Clear();
            if (!cves.Any())
            {
                var empty = new Label { Text = "No CVEs found", Font = Theme.FontSm, ForeColor = Theme.TextMuted, BackColor = Theme.White, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
                _flowCves.Controls.Add(empty);
                return;
            }

            foreach (var cve in cves.OrderByDescending(c => c.CvssScore).Take(5))
            {
                var row = new Panel { Width = 244, Height = 44, BackColor = Theme.White, Margin = new Padding(0, 4, 0, 0) };
                row.Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, row.Height - 1, row.Width, row.Height - 1);

                var dot = new Panel { Size = new Size(8, 8), Location = new Point(0, 10), BackColor = SevColor(cve.Severity) };

                var idLbl = new Label { Text = cve.CveId, Location = new Point(14, 4), Size = new Size(150, 16), Font = Theme.FontSmBold, ForeColor = Theme.TextPrimary, BackColor = Theme.White };
                var sevBadge = new BadgeLabel { Risk = cve.Severity, Location = new Point(172, 4), Size = new Size(68, 18) };
                var descLbl = new Label { Text = cve.Description.Length > 38 ? cve.Description[..35] + "…" : cve.Description, Location = new Point(14, 22), Size = new Size(230, 14), Font = Theme.FontSm, ForeColor = Theme.TextSecondary, BackColor = Theme.White };
                row.Controls.AddRange(new Control[] { dot, idLbl, sevBadge, descLbl });
                _flowCves.Controls.Add(row);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private Color SevColor(string s) => s?.ToUpper() switch
        {
            "CRITICAL" or "HIGH" => Theme.DotInactive,
            "MEDIUM"             => Theme.DotWarning,
            _                   => Theme.DotActive
        };

        private RoundedPanel Card(int h)
        {
            var p = new RoundedPanel { Width = 268, Height = h, BackColor = Theme.White, Margin = new Padding(0) };
            return p;
        }

        private Label HeaderLabel(string text, int x, int y)
            => new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.TextPrimary, BackColor = Theme.White };

        private Panel Spacer(int h) => new Panel { Width = 268, Height = h, BackColor = Theme.Surface2 };

        private static GraphicsPath BarPath(float x, float y, float w, float h, int r)
        {
            float d = r * 2f;
            var p = new GraphicsPath();
            p.AddArc(x, y, d, d, 180, 90);
            p.AddArc(x + w - d, y, d, d, 270, 90);
            p.AddLine(x + w, y + r, x + w, y + h);
            p.AddLine(x, y + h, x, y + r);
            p.CloseFigure();
            return p;
        }
    }
}
