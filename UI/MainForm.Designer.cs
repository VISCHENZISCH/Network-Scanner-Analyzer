using NetScanAnalyzer.UI.Controls;
using NetScanAnalyzer.UI.Panels;

namespace NetScanAnalyzer.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null!;

        // Panels
        private HostsSidebar     _sidebar  = null!;
        private ScannerMainPanel _scanner  = null!;
        private RightSidePanel   _right    = null!;
        private Panels.SnifferPanel _capture = null!;

        // Nav tabs
        private NavTab[] _tabs = null!;

        // Top bar
        private Panel _topBar = null!;
        private CircularProgressRing _ring = null!;
        private Label _lblApp = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            SuspendLayout();

            // ── Form ─────────────────────────────────────────────────────────
            Text            = "NetScan Analyzer";
            Size            = new Size(1320, 820);
            MinimumSize     = new Size(1100, 700);
            BackColor       = Theme.Surface2;
            StartPosition   = FormStartPosition.CenterScreen;
            Font            = Theme.FontBase;
            ForeColor       = Theme.TextPrimary;
            DoubleBuffered  = true;

            // ── Top Bar (56px) ───────────────────────────────────────────────
            _topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.White,
                Padding = new Padding(0),
            };
            _topBar.Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, 55, _topBar.Width, 55);

            // Logo left
            var pnlLogo = new Panel { Width = 230, Height = 56, BackColor = Theme.White };
            pnlLogo.Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 229, 12, 229, 44);

            // Vector logo panel (32×32 rounded square)
            var logoBox = new Panel { Location = new Point(14, 12), Size = new Size(32, 32), BackColor = Theme.White };
            logoBox.Paint += (_, e) => Icons.AppLogo(e.Graphics, new RectangleF(0, 0, 32, 32));
            _lblApp = new Label { Text = "NetScan Analyzer", Location = new Point(54, 17), AutoSize = true, Font = Theme.FontLg, ForeColor = Theme.TextPrimary, BackColor = Theme.White };
            pnlLogo.Controls.AddRange(new Control[] { logoBox, _lblApp });

            // Nav tabs with vector icons
            var tabNames = new[] { "Discovery", "Port Scan", "Services", "Capture", "Reports" };
            _tabs = new NavTab[tabNames.Length];
            var pnlTabs = new Panel { BackColor = Theme.White, Height = 56 };
            Action<Graphics, RectangleF, Color>[] tabIcons = {
                Icons.NavDiscovery, Icons.NavPortScan, Icons.NavServices, Icons.NavCapture, Icons.NavReports };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int idx = i;
                _tabs[i] = new NavTab { Text = tabNames[i], Width = tabNames[i].Length > 7 ? 115 : 100, Height = 56, BackColor = Theme.White, IconDraw = tabIcons[i] };
                _tabs[i].Click += (_, _) => SwitchTab(idx);
                pnlTabs.Controls.Add(_tabs[i]);
            }
            _tabs[0].Selected = true;
            ArrangeTabsHorizontally(pnlTabs);

            // Progress ring right
            _ring = new CircularProgressRing { Size = new Size(44, 44), Visible = false, BackColor = Theme.White, RingThickness = 5f };
            var pnlRight = new Panel { Width = 120, Height = 56, BackColor = Theme.White };
            _ring.Location = new Point(20, 6);
            pnlRight.Controls.Add(_ring);

            // Layout the top bar using manual positioning with Resize
            _topBar.Controls.AddRange(new Control[] { pnlLogo, pnlTabs, pnlRight });
            _topBar.SizeChanged += (_, _) =>
            {
                pnlTabs.Left  = (int)((_topBar.Width - pnlTabs.Width) / 2f);
                pnlTabs.Top   = 0;
                pnlRight.Left = _topBar.Width - 130;
                pnlRight.Top  = 0;
            };
            _topBar.Width = Size.Width;
            pnlTabs.Width = _tabs.Sum(t => t.Width) + 10;
            ArrangeTabsHorizontally(pnlTabs);

            // ── Status bar ───────────────────────────────────────────────────
            var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 32, BackColor = Theme.Surface2 };
            statusBar.Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, 0, statusBar.Width, 0);

            var lblStatus     = new Label { Name = "lblStatusMsg",    Text = "Ready",               Dock = DockStyle.Left, Width = 300, Font = Theme.FontSm, ForeColor = Theme.TextSecondary, BackColor = Theme.Surface2, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0) };
            var lblPorts      = new Label { Name = "lblStatusPorts",  Text = "0 open ports",        Dock = DockStyle.Left, Width = 140, Font = Theme.FontSm, ForeColor = Theme.TextSecondary, BackColor = Theme.Surface2, TextAlign = ContentAlignment.MiddleLeft };
            var lblLive       = new Label { Name = "lblStatusLive",   Text = "0 live hosts",        Dock = DockStyle.Left, Width = 120, Font = Theme.FontSm, ForeColor = Theme.TextSecondary, BackColor = Theme.Surface2, TextAlign = ContentAlignment.MiddleLeft };
            var lblEngine     = new Label { Name = "lblStatusEngine", Text = "● Standard Engine",   Dock = DockStyle.Right, Width = 160, Font = Theme.FontSm, ForeColor = Theme.DotActive,    BackColor = Theme.Surface2, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 16, 0) };
            statusBar.Controls.AddRange(new Control[] { lblEngine, lblLive, lblPorts, lblStatus });

            // ── Content panels ───────────────────────────────────────────────
            _sidebar = new HostsSidebar();
            _sidebar.HostSelected += h => _right.ShowHostDetails(h);

            _scanner = new ScannerMainPanel();
            _scanner.HostSelected    += h => _right.ShowHostDetails(h);
            _scanner.ExportRequested += fmt => Export(fmt);
            _scanner.NewScanRequested += () => _ = OpenNewScanDialogAsync();

            _right = new RightSidePanel();

            _capture = new Panels.SnifferPanel { Dock = DockStyle.Fill, Visible = false };

            var center = new Panel { Dock = DockStyle.Fill, BackColor = Theme.White };
            center.Controls.Add(_capture);
            center.Controls.Add(_scanner);

            // ── Assembly ─────────────────────────────────────────────────────
            Controls.Add(center);
            Controls.Add(_right);
            Controls.Add(_sidebar);
            Controls.Add(statusBar);
            Controls.Add(_topBar);

            ResumeLayout(false);
        }

        private void ArrangeTabsHorizontally(Panel container)
        {
            int x = 0;
            foreach (Control c in container.Controls)
            {
                c.Location = new Point(x, 0);
                c.Height   = 56;
                x += c.Width;
            }
            container.Height = 56;
        }

        private void SwitchTab(int idx)
        {
            for (int i = 0; i < _tabs.Length; i++) _tabs[i].Selected = i == idx;
            _scanner.Visible = idx != 3;
            _capture.Visible = idx == 3;
            _right.Visible   = idx != 3;
        }
    }
}
