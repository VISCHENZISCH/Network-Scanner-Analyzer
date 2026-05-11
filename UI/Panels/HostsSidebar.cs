using NetScanAnalyzer.Models;
using NetScanAnalyzer.UI.Controls;

namespace NetScanAnalyzer.UI.Panels
{
    public class HostsSidebar : UserControl
    {
        private TreeView _tree = null!;
        private SectionHeader _header = null!;
        private Panel _pnlLegend = null!;

        public event Action<NetworkHost>? HostSelected;

        private List<NetworkHost> _hosts = new();

        public HostsSidebar()
        {
            Dock = DockStyle.Left;
            Width = 260;
            BackColor = Theme.Surface2;
            Padding = new Padding(0);
            DoubleBuffered = true;
            InitUI();
        }

        private void InitUI()
        {
            // Right separator
            Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), Width - 1, 0, Width - 1, Height);

            // Header
            _header = new SectionHeader
            {
                Text = "Hosts",
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Theme.Surface2,
                Padding = new Padding(16, 0, 8, 0)
            };

            // Search box
            var searchBox = new SearchBox
            {
                Dock = DockStyle.Top,
                Height = 36,
                Margin = new Padding(12, 8, 12, 8),
            };

            var searchWrapper = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.Surface2, Padding = new Padding(12, 8, 12, 8) };
            searchBox.Width = 236;
            searchBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            searchWrapper.Controls.Add(searchBox);
            searchBox.TextChanged += (_, _) => FilterTree(searchBox.SearchText);

            // TreeView
            _tree = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface2,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBase,
                BorderStyle = BorderStyle.None,
                ShowLines = false,
                ShowPlusMinus = false,
                ShowRootLines = false,
                FullRowSelect = true,
                HideSelection = false,
                DrawMode = TreeViewDrawMode.OwnerDrawAll,
                ItemHeight = 36,
                Indent = 20,
                Scrollable = true,
            };
            _tree.DrawNode += Tree_DrawNode;
            _tree.AfterSelect += Tree_AfterSelect;
            _tree.MouseMove += Tree_MouseMove;

            // Legend
            _pnlLegend = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Theme.Surface2, Padding = new Padding(16, 0, 16, 0) };
            _pnlLegend.Paint += Legend_Paint;
            _pnlLegend.Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, 0, _pnlLegend.Width, 0);

            Controls.Add(_tree);
            Controls.Add(searchWrapper);
            Controls.Add(_header);
            Controls.Add(_pnlLegend);
        }

        private int _hoveredIndex = -1;
        private void Tree_MouseMove(object? s, MouseEventArgs e)
        {
            var node = _tree.GetNodeAt(e.Location);
            int newIdx = node?.Index ?? -1;
            if (newIdx != _hoveredIndex) { _hoveredIndex = newIdx; _tree.Invalidate(); }
        }

        private void Tree_DrawNode(object? s, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null) return;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            bool selected = (e.State & TreeNodeStates.Selected) != 0;
            bool hovered  = e.Node.Index == _hoveredIndex;

            // Background
            var bg = selected ? Theme.AccentLight : hovered ? Theme.Surface3 : Theme.Surface2;
            g.FillRectangle(new SolidBrush(bg), e.Bounds);

            // Selected: blue left accent bar
            if (selected)
                g.FillRectangle(new SolidBrush(Theme.Accent), e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height);

            int indent = 16 + e.Node.Level * 20;

            if (e.Node.Tag is NetworkHost host)
            {
                // Status dot
                var dotColor = Theme.StatusColor(host.RiskLabel == "HIGH" ? "high" : host.IsOnline ? "active" : "inactive");
                int dotY = e.Bounds.Y + (e.Bounds.Height - 8) / 2;
                g.FillEllipse(new SolidBrush(dotColor), indent, dotY, 8, 8);

                // IP + hostname
                int tx = indent + 14;
                using var ipFont = Theme.FontBold;
                g.DrawString(host.IPAddress, ipFont, new SolidBrush(selected ? Theme.Accent : Theme.TextPrimary), tx, e.Bounds.Y + 6);
                using var hostFont = Theme.FontSm;
                g.DrawString(host.Hostname.Length > 24 ? host.Hostname[..21] + "…" : host.Hostname,
                             hostFont, new SolidBrush(Theme.TextSecondary), tx, e.Bounds.Y + 20);
            }
            else
            {
                // Subnet group node
                bool expanded = e.Node.IsExpanded;
                string arrow = expanded ? "▾" : "▸";
                using var af = new Font("Segoe UI", 9f);
                g.DrawString(arrow, af, new SolidBrush(Theme.TextSecondary), indent - 4, e.Bounds.Y + 10);
                using var sf = Theme.FontSmBold;
                g.DrawString(e.Node.Text, sf, new SolidBrush(Theme.TextSecondary), indent + 14, e.Bounds.Y + 10);

                // Count badge
                if (e.Node.Tag is int count)
                {
                    string cnt = count.ToString();
                    using var bf = Theme.FontSmBold;
                    var bsz = g.MeasureString(cnt, bf);
                    float bw = bsz.Width + 10;
                    float bx = e.Bounds.Right - bw - 8;
                    float by = e.Bounds.Y + (e.Bounds.Height - 18) / 2f;
                    using var path = RoundPath(new RectangleF(bx, by, bw, 18), 9);
                    g.FillPath(new SolidBrush(Theme.AccentLight), path);
                    using var bsf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(cnt, bf, new SolidBrush(Theme.Accent), new RectangleF(bx, by, bw, 18), bsf);
                }
            }

            // Bottom separator
            g.DrawLine(new Pen(Color.FromArgb(10, 0, 0, 0)), e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private void Tree_AfterSelect(object? s, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is NetworkHost host) HostSelected?.Invoke(host);
        }

        private void Legend_Paint(object? s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Theme.Surface2);

            var items = new[] {
                (Theme.DotActive, "Live"),
                (Theme.DotWarning, "At Risk"),
                (Theme.DotInactive, "Down"),
            };
            int x = 16;
            using var f = Theme.FontSm;
            foreach (var (color, label) in items)
            {
                g.FillEllipse(new SolidBrush(color), x, 16, 7, 7);
                g.DrawString(label, f, new SolidBrush(Theme.TextSecondary), x + 11, 11);
                x += (int)g.MeasureString(label, f).Width + 26;
            }
        }

        public void LoadHosts(List<NetworkHost> hosts)
        {
            _hosts = hosts;
            RebuildTree(hosts);
        }

        private void RebuildTree(List<NetworkHost> hosts)
        {
            if (InvokeRequired) { Invoke(() => RebuildTree(hosts)); return; }
            _tree.BeginUpdate();
            _tree.Nodes.Clear();
            _header.Count = hosts.Count;
            _header.Invalidate();

            var subnets = hosts.GroupBy(h =>
            {
                var p = h.IPAddress.Split('.');
                return p.Length >= 3 ? $"{p[0]}.{p[1]}.{p[2]}.0/24" : "Other";
            });

            foreach (var grp in subnets.OrderBy(g => g.Key))
            {
                var sub = new TreeNode(grp.Key) { Tag = grp.Count() };
                foreach (var host in grp.OrderBy(h => h.IPAddress, StringComparer.Ordinal))
                {
                    var n = new TreeNode(host.IPAddress) { Tag = host, ToolTipText = host.Hostname };
                    sub.Nodes.Add(n);
                }
                _tree.Nodes.Add(sub);
                sub.Expand();
            }

            _tree.EndUpdate();
        }

        private void FilterTree(string query)
        {
            var filtered = string.IsNullOrWhiteSpace(query)
                ? _hosts
                : _hosts.Where(h => h.IPAddress.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                    h.Hostname.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            RebuildTree(filtered);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundPath(RectangleF r, int rad)
        {
            float d = rad * 2f;
            var p = new System.Drawing.Drawing2D.GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
