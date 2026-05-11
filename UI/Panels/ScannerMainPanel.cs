using NetScanAnalyzer.Models;
using NetScanAnalyzer.UI.Controls;
using System.Drawing.Drawing2D;

namespace NetScanAnalyzer.UI.Panels
{
    public class ScannerMainPanel : UserControl
    {
        private DataGridView _grid = null!;
        private SearchBox _search = null!;
        private RoundedButton _btnScan = null!, _btnExport = null!;
        private Label _lblCount = null!;
        private Panel _pnlToolbar = null!;

        public event Action<NetworkHost>? HostSelected;
        public event Action<string>? ExportRequested;
        public event Action? NewScanRequested;

        private List<NetworkHost> _all = new(), _filtered = new();

        public ScannerMainPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.White;
            DoubleBuffered = true;
            InitUI();
        }

        private void InitUI()
        {
            // ── Toolbar ──────────────────────────────────────────────────────
            _pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Theme.White,
            };
            _pnlToolbar.Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, _pnlToolbar.Height - 1, _pnlToolbar.Width, _pnlToolbar.Height - 1);

            _btnScan = new RoundedButton { Text = "+  New Scan", Width = 140, Height = 40, Location = new Point(20, 16) };
            _btnScan.Click += (_, _) => NewScanRequested?.Invoke();

            _btnExport = new RoundedButton { Text = "⬆  Export", Width = 125, Height = 40, Location = new Point(172, 16), NormalColor = Theme.White, ForeColor = Theme.TextPrimary, IsOutline = true };
            _btnExport.Click += ShowExportMenu;

            _search = new SearchBox { Location = new Point(316, 16), Height = 40, Width = 300, Placeholder = "Search IP, hostname, OS..." };
            _search.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _search.TextChanged += (_, _) => Filter(_search.SearchText);

            _lblCount = new Label { Text = "0 hosts", Font = Theme.FontSm, ForeColor = Theme.TextSecondary, BackColor = Theme.White, AutoSize = true };
            _lblCount.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            _pnlToolbar.Controls.AddRange(new Control[] { _btnScan, _btnExport, _search, _lblCount });
            _pnlToolbar.SizeChanged += (_, _) => { _lblCount.Location = new Point(_pnlToolbar.Width - _lblCount.Width - 20, 28); };

            // ── DataGridView ─────────────────────────────────────────────────
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 44,
                EnableHeadersVisualStyles = false,
                GridColor = Theme.Border,
                RowTemplate = { Height = 50 },
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                ScrollBars = ScrollBars.Vertical,
                // Default cell style
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.White,
                    ForeColor = Theme.TextPrimary,
                    SelectionBackColor = Theme.AccentLight,
                    SelectionForeColor = Theme.TextPrimary,
                    Padding = new Padding(12, 0, 12, 0),
                    Font = Theme.FontBase,
                },
                // Alternating rows
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.RowAlt,
                    ForeColor = Theme.TextPrimary,
                    SelectionBackColor = Theme.AccentLight,
                    SelectionForeColor = Theme.TextPrimary,
                },
                // Headers
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Surface2,
                    ForeColor = Theme.HeaderText,
                    SelectionBackColor = Theme.Surface2,
                    SelectionForeColor = Theme.HeaderText,
                    Font = Theme.FontSmBold,
                    Padding = new Padding(12, 0, 12, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            };

            // Columns
            _grid.Columns.AddRange(
                TxtCol("IP Address",  "IP",    190, DataGridViewAutoSizeColumnMode.None),
                TxtCol("Open Ports",  "Ports", 120, DataGridViewAutoSizeColumnMode.None),
                TxtCol("OS Detected", "OS",    200, DataGridViewAutoSizeColumnMode.Fill),
                TxtCol("Risk Level",  "Risk",  130, DataGridViewAutoSizeColumnMode.None),
                TxtCol("Last Scan",   "Scan",  180, DataGridViewAutoSizeColumnMode.None)
            );

            // Events
            _grid.CellPainting       += Grid_CellPainting;
            _grid.RowPrePaint        += Grid_RowPrePaint;
            _grid.SelectionChanged   += Grid_SelectionChanged;
            _grid.ColumnHeaderMouseClick += (s, e) => SortByColumn(e.ColumnIndex);
            _grid.Sorted             += (_, _) => { };

            // Status bar
            var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Theme.Surface2, Padding = new Padding(16, 0, 16, 0) };
            statusBar.Paint += (_, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, 0, statusBar.Width, 0);
            var lblStatus = new Label { Text = "Ready", Font = Theme.FontSm, ForeColor = Theme.TextSecondary, BackColor = Theme.Surface2, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            statusBar.Controls.Add(lblStatus);

            Controls.Add(_grid);
            Controls.Add(_pnlToolbar);
            Controls.Add(statusBar);
        }

        // ── Cell Custom Painting ─────────────────────────────────────────────
        private void Grid_CellPainting(object? s, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _filtered.Count) return;
            var host = _filtered[e.RowIndex];

            if (_grid.Columns[e.ColumnIndex].Name == "Risk")
            {
                e.PaintBackground(e.ClipBounds, true);
                DrawBadge(e.Graphics!, host.RiskLabel, e.CellBounds);
                e.Handled = true;
            }
            else if (_grid.Columns[e.ColumnIndex].Name == "OS")
            {
                e.PaintBackground(e.ClipBounds, true);
                DrawOsCell(e.Graphics!, host.OSFamily, e.CellBounds, e.RowIndex);
                e.Handled = true;
            }
            else if (_grid.Columns[e.ColumnIndex].Name == "IP")
            {
                e.PaintBackground(e.ClipBounds, true);
                DrawIpCell(e.Graphics!, host, e.CellBounds, e.RowIndex);
                e.Handled = true;
            }
        }

        private void Grid_RowPrePaint(object? s, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _filtered.Count) return;
            bool sel = _grid.Rows[e.RowIndex].Selected;
            if (sel)
            {
                // Blue left accent bar on selected row
                var g = e.Graphics;
                var bounds = _grid.GetRowDisplayRectangle(e.RowIndex, false);
                g.FillRectangle(new SolidBrush(Theme.Accent), bounds.X, bounds.Y, 3, bounds.Height);
            }
        }

        private void DrawBadge(Graphics g, string risk, Rectangle bounds)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var (bg, fg) = Theme.BadgeColors(risk);
            float bw = 72f, bh = 22f;
            float bx = bounds.X + (bounds.Width - bw) / 2;
            float by = bounds.Y + (bounds.Height - bh) / 2;
            var rect = new RectangleF(bx, by, bw, bh);
            using var path = RoundPath(rect, 11);
            g.FillPath(new SolidBrush(bg), path);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(risk.ToUpper(), Theme.FontSmBold, new SolidBrush(fg), rect, sf);
        }

        private void DrawOsCell(Graphics g, string os, Rectangle bounds, int row)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // Vector OS icon
            var iconRect = new RectangleF(bounds.X + 8, bounds.Y + (bounds.Height - 20f) / 2f, 20f, 20f);
            Icons.OsIcon(g, iconRect, os);
            // OS name text
            using var f = Theme.FontBase;
            g.DrawString(os, f, new SolidBrush(Theme.TextPrimary), bounds.X + 34, bounds.Y + (bounds.Height - f.Height) / 2f);
        }

        private void DrawIpCell(Graphics g, NetworkHost host, Rectangle bounds, int row)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Status dot
            var dotColor = host.IsOnline ? Theme.DotActive : Theme.DotInactive;
            int dotY = bounds.Y + (bounds.Height - 8) / 2;
            g.FillEllipse(new SolidBrush(dotColor), bounds.X + 12, dotY, 8, 8);
            using var f = Theme.FontBold;
            g.DrawString(host.IPAddress, f, new SolidBrush(_grid.Rows[row].Selected ? Theme.Accent : Theme.TextPrimary), bounds.X + 26, bounds.Y + (bounds.Height - f.Height) / 2f);
        }

        // ── Sort ─────────────────────────────────────────────────────────────
        private string _sortCol = "IP";
        private bool _sortAsc = true;
        private void SortByColumn(int colIdx)
        {
            string col = _grid.Columns[colIdx].Name;
            if (_sortCol == col) _sortAsc = !_sortAsc;
            else { _sortCol = col; _sortAsc = true; }
            Filter(_search.SearchText);
        }

        // ── Selection ────────────────────────────────────────────────────────
        private void Grid_SelectionChanged(object? s, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0) return;
            int idx = _grid.SelectedRows[0].Index;
            if (idx >= 0 && idx < _filtered.Count) HostSelected?.Invoke(_filtered[idx]);
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void SetHosts(List<NetworkHost> hosts)
        {
            if (InvokeRequired) { Invoke(() => SetHosts(hosts)); return; }
            _all = hosts;
            Filter(_search.SearchText);
        }

        private void Filter(string q)
        {
            if (InvokeRequired) { Invoke(() => Filter(q)); return; }
            _filtered = string.IsNullOrWhiteSpace(q)
                ? _all.ToList()
                : _all.Where(h =>
                    h.IPAddress.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    h.Hostname.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    h.OSFamily.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            // Sort
            _filtered = _sortCol switch
            {
                "Ports" => _sortAsc ? _filtered.OrderBy(h => h.OpenPorts.Count).ToList() : _filtered.OrderByDescending(h => h.OpenPorts.Count).ToList(),
                "Risk"  => _sortAsc ? _filtered.OrderBy(h => h.RiskScore).ToList() : _filtered.OrderByDescending(h => h.RiskScore).ToList(),
                "Scan"  => _sortAsc ? _filtered.OrderBy(h => h.LastScanned).ToList() : _filtered.OrderByDescending(h => h.LastScanned).ToList(),
                _       => _sortAsc ? _filtered.OrderBy(h => h.IPAddress, StringComparer.Ordinal).ToList() : _filtered.OrderByDescending(h => h.IPAddress, StringComparer.Ordinal).ToList(),
            };

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            _grid.SuspendLayout();
            _grid.Rows.Clear();

            foreach (var h in _filtered)
            {
                _grid.Rows.Add(
                    h.IPAddress,
                    h.OpenPorts.Count,
                    h.OSFamily,
                    h.RiskLabel,
                    h.LastScanned.ToString("MMM d, yyyy  HH:mm")
                );
            }

            _lblCount.Text = $"{_filtered.Count} host{(_filtered.Count != 1 ? "s" : "")}";
            _grid.ResumeLayout();
        }

        private void ShowExportMenu(object? s, EventArgs e)
        {
            var menu = new ContextMenuStrip { Font = Theme.FontBase, BackColor = Theme.White };
            void Add(string t, string f) => menu.Items.Add(t, null, (_, _) => ExportRequested?.Invoke(f));
            Add("Export as JSON",   "JSON");
            Add("Export as XML",    "XML");
            Add("Export as CSV",    "CSV");
            Add("Export as PDF",    "PDF");
            menu.Items.Add(new ToolStripSeparator());
            Add("Risk Report (HTML)", "HTML");
            menu.Show(_btnExport, new Point(0, _btnExport.Height + 2));
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static DataGridViewTextBoxColumn TxtCol(string header, string name, int w, DataGridViewAutoSizeColumnMode auto)
            => new() { HeaderText = header, Name = name, Width = w, AutoSizeMode = auto, SortMode = DataGridViewColumnSortMode.Programmatic };

        private static GraphicsPath RoundPath(RectangleF r, int rad)
        {
            float d = rad * 2f;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
