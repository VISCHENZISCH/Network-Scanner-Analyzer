using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace NetScanAnalyzer.UI.Controls
{
    // ─────────────────────────────────────────────────────────────────────────
    // RoundedButton — #2563EB background, white text, 6px radius, hover #1D4ED8
    // ─────────────────────────────────────────────────────────────────────────
    public class RoundedButton : Button
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 6;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color NormalColor { get; set; } = Theme.Accent;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverColor { get; set; } = Theme.AccentHover;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsOutline { get; set; } = false;

        private bool _hovering;

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            BackColor = Theme.White;
            ForeColor = Theme.White;
            Font = Theme.FontBold;
            Cursor = Cursors.Hand;
            Height = 36;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override void OnMouseEnter(EventArgs e) { _hovering = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovering = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.White);

            Color bg = IsOutline ? Theme.White : (_hovering ? HoverColor : NormalColor);
            using var path = Rounded(ClientRectangle, CornerRadius);

            if (IsOutline)
            {
                using var borderBrush = new SolidBrush(Theme.Accent);
                using var pen = new Pen(Theme.Accent, 1.5f);
                g.FillPath(new SolidBrush(bg), path);
                g.DrawPath(pen, path);
            }
            else
            {
                g.FillPath(new SolidBrush(bg), path);
            }

            var fc = IsOutline ? Theme.Accent : Theme.White;
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(Text, Font, new SolidBrush(fc), ClientRectangle, sf);
        }

        private static GraphicsPath Rounded(Rectangle r, int rad)
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

    // ─────────────────────────────────────────────────────────────────────────
    // RoundedPanel — 6px corners, 1px #E5E7EB border, configurable background
    // ─────────────────────────────────────────────────────────────────────────
    public class RoundedPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 6;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DrawBorder { get; set; } = true;

        public RoundedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Theme.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.Surface2);

            using var path = Rounded(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
            g.FillPath(new SolidBrush(BackColor), path);

            if (DrawBorder)
                using (var pen = new Pen(Theme.Border, 1f))
                    g.DrawPath(pen, path);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        private static GraphicsPath Rounded(Rectangle r, int rad)
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

    // ─────────────────────────────────────────────────────────────────────────
    // BadgeLabel — risk badge with rounded 12px background
    // ─────────────────────────────────────────────────────────────────────────
    public class BadgeLabel : Control
    {
        private string _risk = "LOW";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Risk
        {
            get => _risk;
            set { _risk = value; Invalidate(); }
        }

        public BadgeLabel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Size = new Size(72, 22);
            Font = Theme.FontSmBold;
            BackColor = Theme.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.White);

            var (bg, fg) = Theme.BadgeColors(_risk);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = Rounded(rect, 11);
            g.FillPath(new SolidBrush(bg), path);

            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(_risk.ToUpper(), Font, new SolidBrush(fg), rect, sf);
        }

        private static GraphicsPath Rounded(Rectangle r, int rad)
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

    // ─────────────────────────────────────────────────────────────────────────
    // CircularProgressRing — GDI+ arc with configurable value (0-100)
    // ─────────────────────────────────────────────────────────────────────────
    public class CircularProgressRing : Control
    {
        private int _value = 0;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Value { get => _value; set { _value = Math.Clamp(value, 0, 100); Invalidate(); } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float RingThickness { get; set; } = 5f;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowPercent { get; set; } = true;

        public CircularProgressRing()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            Size = new Size(44, 44);
            BackColor = Theme.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            float pad = RingThickness;
            var rect = new RectangleF(pad, pad, Width - pad * 2, Height - pad * 2);

            // Track
            using var trackPen = new Pen(Theme.Surface3, RingThickness);
            g.DrawArc(trackPen, rect, 0, 360);

            // Progress arc
            if (_value > 0)
            {
                float sweep = 360f * _value / 100f;
                using var progPen = new Pen(Theme.Accent, RingThickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawArc(progPen, rect, -90, sweep);
            }

            // Center text
            if (ShowPercent)
            {
                string txt = $"{_value}%";
                using var f = new Font("Segoe UI", Width * 0.2f, FontStyle.Bold, GraphicsUnit.Pixel);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(txt, f, new SolidBrush(Theme.TextPrimary), ClientRectangle, sf);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // StatusDot — 8px filled circle with configurable color
    // ─────────────────────────────────────────────────────────────────────────
    public class StatusDot : Control
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color DotColor { get; set; } = Theme.DotActive;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int DotSize { get; set; } = 8;

        public StatusDot()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            Size = new Size(16, 16);
            BackColor = Theme.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.White);
            int x = (Width - DotSize) / 2, y = (Height - DotSize) / 2;
            g.FillEllipse(new SolidBrush(DotColor), x, y, DotSize, DotSize);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NavTab — top navigation tab with underline indicator + vector icon
    // ─────────────────────────────────────────────────────────────────────────
    public class NavTab : Control
    {
        private bool _selected, _hovered;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Selected { get => _selected; set { _selected = value; Invalidate(); } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<Graphics, RectangleF, Color>? IconDraw { get; set; }

        public NavTab()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            Height = 56; BackColor = Theme.White; Cursor = Cursors.Hand; Font = Theme.FontBase;
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            if (_hovered && !_selected)
                g.FillRectangle(new SolidBrush(Theme.Surface2), ClientRectangle);

            var tc = _selected ? Theme.Accent : Theme.TextSecondary;
            var font = _selected ? Theme.FontBold : Theme.FontBase;

            if (IconDraw != null)
            {
                // Icon left + text right layout
                float totalW = 16f + Text.Length * 7f;
                float startX = (Width - totalW) / 2f;
                var iconRect = new RectangleF(startX, (Height - 16f) / 2f, 16f, 16f);
                IconDraw(g, iconRect, tc);
                g.DrawString(Text, font, new SolidBrush(tc), startX + 20f, (Height - font.Height) / 2f);
            }
            else
            {
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(Text, font, new SolidBrush(tc), ClientRectangle, sf);
            }

            if (_selected)
                g.FillRectangle(new SolidBrush(Theme.Accent), 0, Height - 2, Width, 2);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SectionHeader — section title with count badge
    // ─────────────────────────────────────────────────────────────────────────
    public class SectionHeader : Control
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Count { get; set; } = -1;

        public SectionHeader()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Height = 40;
            BackColor = Theme.Surface2;
            Font = Theme.FontBold;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.Surface2);

            g.DrawString(Text, Font, new SolidBrush(Theme.TextPrimary), 0, (Height - Font.Height) / 2f);

            if (Count >= 0)
            {
                string badge = Count.ToString();
                using var bf = Theme.FontSmBold;
                var bsz = g.MeasureString(badge, bf);
                float bx = g.MeasureString(Text, Font).Width + 8;
                float by = (Height - 18) / 2f;
                var br = new RectangleF(bx, by, Math.Max(24, bsz.Width + 10), 18);
                using var path = RoundPath(br, 9);
                g.FillPath(new SolidBrush(Theme.AccentLight), path);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(badge, bf, new SolidBrush(Theme.Accent), br, sf);
            }

            // Bottom border
            g.DrawLine(new Pen(Theme.Border), 0, Height - 1, Width, Height - 1);
        }

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

    // ─────────────────────────────────────────────────────────────────────────
    // SearchBox — styled text input with search icon
    // ─────────────────────────────────────────────────────────────────────────
    public class SearchBox : Panel
    {
        private readonly TextBox _inner;
        private bool _focused;

        public string SearchText => _inner.Text;
        public event EventHandler? TextChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Placeholder { get; set; } = "Search hosts...";

        public SearchBox()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Height = 36;
            BackColor = Theme.White;
            Padding = new Padding(0);

            _inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.White,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBase,
                Location = new Point(32, 8),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            };
            _inner.GotFocus  += (_, _) => { _focused = true; Invalidate(); };
            _inner.LostFocus += (_, _) => { _focused = false; Invalidate(); };
            _inner.TextChanged += (_, e) => TextChanged?.Invoke(this, e);
            Controls.Add(_inner);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_inner == null) return;
            _inner.Width = Width - 40;
            _inner.Top = (Height - _inner.Height) / 2;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.Surface2);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundPath(rect, 6);
            g.FillPath(new SolidBrush(Theme.White), path);
            using var pen = new Pen(_focused ? Theme.Accent : Theme.Border, 1f);
            g.DrawPath(pen, path);

            // Search icon (simple magnifier)
            using var iconFont = new Font("Segoe UI", 11f);
            g.DrawString("🔍", iconFont, new SolidBrush(Theme.TextMuted), 6, (Height - 18) / 2f - 1);
        }

        private static GraphicsPath RoundPath(Rectangle r, int rad)
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

    // ─────────────────────────────────────────────────────────────────────────
    // Divider — 1px horizontal or vertical line in #E5E7EB
    // ─────────────────────────────────────────────────────────────────────────
    public class Divider : Control
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Vertical { get; set; } = false;

        public Divider()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Theme.Surface2;
            if (Vertical) Width = 1; else Height = 1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Vertical) e.Graphics.DrawLine(new Pen(Theme.Border), 0, 0, 0, Height);
            else          e.Graphics.DrawLine(new Pen(Theme.Border), 0, 0, Width, 0);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IconBtn — rounded button with GDI+ vector icon + text label
    // ─────────────────────────────────────────────────────────────────────────
    public class IconBtn : Control
    {
        private readonly Action<Graphics, RectangleF, Color> _icon;
        private readonly Color _iconC, _bg, _bgH;
        private readonly bool _outline;
        private bool _hover, _pressed;

        public IconBtn(string label, Action<Graphics, RectangleF, Color> icon,
                       Color iconC, Color bg, bool outline = false)
        {
            _icon = icon; _iconC = iconC; _bg = bg; _outline = outline;
            _bgH = outline ? Theme.Surface3
                           : Color.FromArgb(Math.Max(0, bg.R - 20), Math.Max(0, bg.G - 20), Math.Max(0, bg.B - 20));
            Text = label; Height = 36; Cursor = Cursors.Hand; Font = Theme.FontSmBold;
            BackColor = Theme.White; DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.White);

            Color eff = !Enabled ? Color.FromArgb(229, 231, 235)
                      : _pressed ? Color.FromArgb(Math.Max(0,_bg.R-35), Math.Max(0,_bg.G-35), Math.Max(0,_bg.B-35))
                      : _hover   ? _bgH : _bg;

            using var path = RR(ClientRectangle);
            g.FillPath(new SolidBrush(eff), path);
            if (_outline) g.DrawPath(new Pen(_bg, 1.5f), path);

            var ic = !Enabled ? Theme.TextMuted : _outline ? _iconC : Color.White;
            _icon(g, new RectangleF(8, (Height - 18f) / 2, 18f, 18f), ic);

            using var sf = new StringFormat { LineAlignment = StringAlignment.Center };
            g.DrawString(Text, Font, new SolidBrush(!Enabled ? Theme.TextMuted : _outline ? _iconC : Color.White),
                new RectangleF(30, 0, Width - 34, Height), sf);
        }

        static GraphicsPath RR(Rectangle r, int rad = 6) {
            float d = rad * 2f; var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right-d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right-d, r.Bottom-d, d, d, 0, 90); p.AddArc(r.X, r.Bottom-d, d, d, 90, 90);
            p.CloseFigure(); return p; }
    }
}
