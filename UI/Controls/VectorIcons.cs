using System.Drawing.Drawing2D;

namespace NetScanAnalyzer.UI.Controls
{
    public static class Icons
    {
        // ── Viewport transform (use SVG coords directly) ──────────────────
        static void VP(Graphics g, RectangleF r, float vw, float vh, Action<Graphics> draw)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var old = g.Transform.Clone();
            float s = Math.Min(r.Width / vw, r.Height / vh);
            g.TranslateTransform(r.X + (r.Width - vw * s) / 2f, r.Y + (r.Height - vh * s) / 2f);
            g.ScaleTransform(s, s);
            draw(g); g.Transform = old;
        }

        // ── Primitive helpers ─────────────────────────────────────────────
        static Pen P(string hex, float w) => P(HC(hex), w);
        static Pen P(Color c, float w) => new(c, w) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        static SolidBrush B(string hex) => new(HC(hex));
        static SolidBrush B(Color c) => new(c);
        static Color HC(string hex) { hex = hex.TrimStart('#'); return Color.FromArgb(255, Convert.ToInt32(hex[..2], 16), Convert.ToInt32(hex[2..4], 16), Convert.ToInt32(hex[4..6], 16)); }
        static void Circle(Graphics g, float cx, float cy, float r, Color fill) => g.FillEllipse(B(fill), cx - r, cy - r, r * 2, r * 2);
        static void CircleS(Graphics g, float cx, float cy, float r, string hex, float w) => g.DrawEllipse(P(hex, w), cx - r, cy - r, r * 2, r * 2);
        static void RR(Graphics g, float x, float y, float w, float h, float rad, Color fill) { using var p = RRP(x, y, w, h, rad); g.FillPath(B(fill), p); }
        static void RRS(Graphics g, float x, float y, float w, float h, float rad, Color stroke, float sw) { using var p = RRP(x, y, w, h, rad); g.DrawPath(P(stroke, sw), p); }
        static GraphicsPath RRP(float x, float y, float w, float h, float r) {
            float d = r * 2; var p = new GraphicsPath();
            p.AddArc(x, y, d, d, 180, 90); p.AddArc(x + w - d, y, d, d, 270, 90);
            p.AddArc(x + w - d, y + h - d, d, d, 0, 90); p.AddArc(x, y + h - d, d, d, 90, 90);
            p.CloseFigure(); return p; }

        // ═══════════════════════════════════════════════════════════════════
        // OS ICONS (28×28 viewBox)
        // ═══════════════════════════════════════════════════════════════════

        public static void OsWindows(Graphics g, RectangleF r) => VP(g, r, 28, 28, g2 => {
            var c = HC("0078D4");
            RR(g2, 4, 4, 9, 9, 1, c); RR(g2, 15, 4, 9, 9, 1, c);
            RR(g2, 4, 15, 9, 9, 1, c); RR(g2, 15, 15, 9, 9, 1, c); });

        public static void OsUbuntu(Graphics g, RectangleF r) => VP(g, r, 28, 28, g2 => {
            Circle(g2, 14, 14, 9, HC("E95420")); Circle(g2, 14, 14, 5, Color.White); Circle(g2, 14, 14, 2.5f, HC("E95420")); });

        public static void OsDebian(Graphics g, RectangleF r) => VP(g, r, 28, 28, g2 => {
            using var path = new GraphicsPath();
            path.AddBezier(14,5, 8,9, 8,18.09f, 14,21);
            path.AddBezier(14,21, 17.31f,21, 20,18.09f, 20,14.5f);
            path.AddBezier(20,14.5f, 20,9, 14,5, 14,5);
            path.CloseFigure();
            g2.FillPath(B("294172"), path);
            // Right half highlight
            using var p2 = new GraphicsPath();
            p2.AddBezier(14,5, 14,5, 20,9, 20,14.5f);
            p2.AddBezier(20,14.5f, 20,18.09f, 17.31f,21, 14,21); p2.CloseFigure();
            g2.FillPath(B("4F92CD"), p2);
            g2.FillEllipse(B(Color.FromArgb(230,255,255,255)), 11,11.5f, 6,7); });

        public static void OsRedHat(Graphics g, RectangleF r) => VP(g, r, 28, 28, g2 => {
            var pts = new[] { new PointF(14,5), new PointF(6,10), new PointF(6,18), new PointF(14,23), new PointF(22,18), new PointF(22,10) };
            g2.FillPolygon(B("CC0000"), pts);
            var right = new[] { new PointF(14,5), new PointF(22,10), new PointF(22,18), new PointF(14,23) };
            g2.FillPolygon(B("990000"), right);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g2.DrawString("RH", new Font("Segoe UI", 5.5f, FontStyle.Bold), B(Color.White), new RectangleF(6, 9, 16, 14), sf); });

        public static void OsUnknown(Graphics g, RectangleF r) => VP(g, r, 28, 28, g2 => {
            CircleS(g2, 14, 14, 9, "A8B0C0", 1.5f);
            Circle(g2, 14, 11, 3, HC("6B7280"));
            g2.DrawArc(P("6B7280", 1.5f), 8, 15, 12, 12, 0, -180); });

        public static void OsCisco(Graphics g, RectangleF r) => VP(g, r, 28, 28, g2 => {
            RRS(g2, 7, 8, 14, 10, 1, HC("1D6FA4"), 1.5f);
            g2.DrawLine(P("1D6FA4", 1.5f), 14, 18, 14, 21); g2.DrawLine(P("1D6FA4", 1.5f), 10, 21, 18, 21);
            g2.DrawLine(P("1D6FA4", 1f), 9, 12, 19, 12); g2.DrawLine(P("1D6FA4", 1f), 9, 15, 15, 15); });

        public static void OsMacos(Graphics g, RectangleF r) => VP(g, r, 28, 28, g2 => {
            RRS(g2, 5, 7, 18, 14, 2, HC("555555"), 1.5f);
            g2.DrawLine(P("555555", 1f), 5, 11, 23, 11);
            Circle(g2, 8.5f, 9, 1f, HC("EF4444")); Circle(g2, 11.5f, 9, 1f, HC("F59E0B")); Circle(g2, 14.5f, 9, 1f, HC("22C55E")); });

        public static void OsAndroid(Graphics g, RectangleF r) => VP(g, r, 28, 28, g2 => {
            var c = HC("3DDC84");
            // Antenna left
            g2.DrawLine(P(c, 1.5f), 10, 6, 8, 4); g2.DrawLine(P(c, 1.5f), 18, 6, 20, 4);
            // Body
            RR(g2, 8, 12, 12, 9, 2, c);
            // Wheels
            RR(g2, 6, 14, 2.5f, 4, 1, c); RR(g2, 19.5f, 14, 2.5f, 4, 1, c);
            Circle(g2, 11, 16.5f, 1, Color.White); Circle(g2, 17, 16.5f, 1, Color.White); });

        public static void OsBsd(Graphics g, RectangleF r) => VP(g, r, 28, 28, g2 => {
            using var path = new GraphicsPath();
            path.AddBezier(14,5, 7,8, 7,15, 14,23); path.AddBezier(14,23, 17.87f,23, 21,19.42f, 21,15);
            path.AddBezier(21,15, 21,8, 14,5, 14,5); path.CloseFigure();
            g2.FillPath(B(Color.Transparent), path); g2.DrawPath(P("AB2B2B", 1.5f), path);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g2.DrawString("BSD", new Font("Segoe UI", 5.5f, FontStyle.Bold), B("AB2B2B"), new RectangleF(7,11,14,12), sf); });

        // ═══════════════════════════════════════════════════════════════════
        // NAV TAB ICONS (22×22 viewBox)
        // ═══════════════════════════════════════════════════════════════════

        public static void NavDiscovery(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            var pen = P(c, 1.5f);
            CircleS(g2, 11, 11, 8, c.Name == "ffffffff" ? "FFFFFF" : ColorTranslator.ToHtml(c).TrimStart('#'), 1.5f);
            CircleS(g2, 11, 11, 3, c.Name == "ffffffff" ? "FFFFFF" : ColorTranslator.ToHtml(c).TrimStart('#'), 1.5f);
            g2.DrawLine(pen, 11, 3, 11, 8); g2.DrawLine(pen, 11, 14, 11, 19);
            g2.DrawLine(pen, 3, 11, 8, 11); g2.DrawLine(pen, 14, 11, 19, 11); });

        public static void NavPortScan(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            var pen = P(c, 1.5f);
            RRS(g2, 3, 6, 16, 10, 2, c, 1.5f);
            g2.DrawLine(pen, 7, 10, 9, 10); g2.DrawLine(pen, 7, 13, 11, 13);
            g2.DrawEllipse(P(c, 1.2f), 13, 9.5f, 4, 4); });

        public static void NavServices(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            RRS(g2, 3, 3, 7, 7, 1.5f, c, 1.5f); RRS(g2, 12, 3, 7, 7, 1.5f, c, 1.5f); RRS(g2, 3, 12, 7, 7, 1.5f, c, 1.5f);
            g2.DrawLine(P(c, 1.5f), 15.5f, 12, 15.5f, 19); g2.DrawLine(P(c, 1.5f), 12, 15.5f, 19, 15.5f); });

        public static void NavCapture(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            var pen = P(c, 1.5f);
            g2.DrawPolygon(pen, new[] { new PointF(3,7), new PointF(11,4), new PointF(19,7), new PointF(19,15), new PointF(11,18), new PointF(3,15) });
            g2.DrawLine(P(c, 1f), 11, 4, 11, 18); g2.DrawLine(P(c, 1f), 3, 7, 19, 7); });

        public static void NavReports(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            var pen = P(c, 1.5f);
            g2.DrawPolygon(pen, new[] { new PointF(6,3), new PointF(16,3), new PointF(19,7), new PointF(19,19), new PointF(3,19), new PointF(3,7) });
            g2.DrawLine(P(c, 1f), 3, 7, 19, 7);
            g2.DrawLine(pen, 7, 11, 15, 11); g2.DrawLine(pen, 7, 14, 12, 14); });

        // ═══════════════════════════════════════════════════════════════════
        // TOOLBAR ICONS (22×22)
        // ═══════════════════════════════════════════════════════════════════

        public static void Search(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            CircleS(g2, 10, 10, 6, ColorTranslator.ToHtml(c).TrimStart('#'), 1.5f);
            g2.DrawLine(P(c, 1.5f), 14.5f, 14.5f, 19, 19); });

        public static void Filter(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            var pen = P(c, 1.5f);
            g2.DrawLine(pen, 4, 6, 18, 6); g2.DrawLine(pen, 7, 11, 15, 11); g2.DrawLine(pen, 10, 16, 12, 16); });

        public static void Export(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            var pen = P(c, 1.5f);
            g2.DrawLine(pen, 11, 3, 11, 14); g2.DrawLine(pen, 7, 10, 11, 14); g2.DrawLine(pen, 15, 10, 11, 14);
            g2.DrawLine(pen, 4, 17, 18, 17); });

        public static void Trash(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            var pen = P(c, 1.5f);
            g2.DrawLine(pen, 4, 6, 18, 6); g2.DrawLine(pen, 8, 3, 14, 3);
            RRS(g2, 5, 7, 12, 12, 1, c, 1.5f);
            g2.DrawLine(P(c, 1f), 11, 10, 11, 16); });

        public static void Download(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            var pen = P(c, 1.5f);
            g2.DrawLine(pen, 11, 3, 11, 14); g2.DrawLine(pen, 7, 10, 11, 14); g2.DrawLine(pen, 15, 10, 11, 14);
            g2.DrawLine(pen, 4, 17, 18, 17); });

        public static void Settings(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            CircleS(g2, 11, 11, 3, ColorTranslator.ToHtml(c).TrimStart('#'), 1.5f);
            var pen = P(c, 1.5f);
            foreach (var (x1,y1,x2,y2) in new[]{(11f,3f,11f,5f),(11f,17f,11f,19f),(3f,11f,5f,11f),(17f,11f,19f,11f),(5.64f,5.64f,7.05f,7.05f),(14.95f,14.95f,16.36f,16.36f),(5.64f,16.36f,7.05f,14.95f),(14.95f,7.05f,16.36f,5.64f)})
                g2.DrawLine(pen, x1,y1,x2,y2); });

        public static void Play(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 =>
            g2.FillPolygon(B(c), new[] { new PointF(6,4), new PointF(18,11), new PointF(6,18) }));

        public static void Stop(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 =>
            g2.FillRectangle(B(c), 5, 5, 12, 12));

        public static void Wifi(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            float cx = 11, cy = 19;
            foreach (var ri in new[] { 4.5f, 7.5f, 10.5f })
                g2.DrawArc(P(c, 1.8f), cx - ri, cy - ri, ri * 2, ri * 2, 205, 130);
            Circle(g2, cx, cy, 1.5f, c); });

        // ═══════════════════════════════════════════════════════════════════
        // STAT CARD ICONS (22×22)
        // ═══════════════════════════════════════════════════════════════════

        public static void StatHostsScanned(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            RR(g2, 3, 8, 5, 11, 1, Color.FromArgb(180, c)); RR(g2, 9, 5, 5, 14, 1, c); RR(g2, 15, 11, 5, 8, 1, Color.FromArgb(128, c)); });

        public static void StatLiveHosts(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            RRS(g2, 4, 6, 14, 10, 2, c, 1.5f);
            g2.DrawLine(P(c, 1f), 4, 9, 18, 9);
            Circle(g2, 7, 7.5f, 0.8f, c); Circle(g2, 9.5f, 7.5f, 0.8f, c);
            g2.DrawLine(P(c, 1.5f), 8, 12, 10, 14); g2.DrawLine(P(c, 1.5f), 10, 14, 14, 11); });

        public static void StatOpenPorts(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            RRS(g2, 4, 9, 14, 8, 1.5f, c, 1.5f);
            g2.DrawArc(P(c, 1.5f), 5, 5, 12, 8, 180, 180);
            Circle(g2, 11, 14, 1.5f, c); });

        public static void StatHighRisk(Graphics g, RectangleF r, Color c) => VP(g, r, 22, 22, g2 => {
            g2.DrawPolygon(P(c, 1.5f), new[] { new PointF(11,3), new PointF(4,6), new PointF(4,12), new PointF(11,19), new PointF(18,12), new PointF(18,6) });
            g2.DrawLine(P(c, 1.5f), 11, 9, 11, 12); Circle(g2, 11, 14.5f, 1, c); });

        // ═══════════════════════════════════════════════════════════════════
        // APP LOGO (32×32 rounded square)
        // ═══════════════════════════════════════════════════════════════════

        public static void AppLogo(Graphics g, RectangleF r)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var bg = RRP(r.X, r.Y, r.Width, r.Height, 8);
            g.FillPath(B(Theme.Accent), bg);
            VP(g, r, 32, 32, g2 => {
                CircleS(g2, 16, 16, 9, "FFFFFF", 1.8f);
                CircleS(g2, 16, 16, 4, "FFFFFF", 1.8f);
                var pen = P(Color.White, 1.8f);
                g2.DrawLine(pen, 16, 7, 16, 12); g2.DrawLine(pen, 16, 20, 16, 25);
                g2.DrawLine(pen, 7, 16, 12, 16); g2.DrawLine(pen, 20, 16, 25, 16); });
        }

        // ── OS icon dispatcher ────────────────────────────────────────────
        public static void OsIcon(Graphics g, RectangleF r, string os)
        {
            Action<Graphics, RectangleF> draw = os.ToLower() switch {
                var s when s.Contains("windows") => OsWindows,
                var s when s.Contains("ubuntu")  => OsUbuntu,
                var s when s.Contains("debian")  => OsDebian,
                var s when s.Contains("red hat") || s.Contains("redhat") => OsRedHat,
                var s when s.Contains("cisco")   => OsCisco,
                var s when s.Contains("macos") || s.Contains("mac os") => OsMacos,
                var s when s.Contains("android") => OsAndroid,
                var s when s.Contains("bsd")     => OsBsd,
                _                                => OsUnknown
            };
            draw(g, r);
        }
    }
}
