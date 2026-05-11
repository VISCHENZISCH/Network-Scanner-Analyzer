namespace NetScanAnalyzer.UI
{
    /// <summary>
    /// Centralized design token system — single source of truth for all UI colors and fonts.
    /// NEVER use SystemColors or unset properties.
    /// </summary>
    public static class Theme
    {
        // ── Surfaces ─────────────────────────────────────────────────────────
        public static Color White    => Color.FromArgb(0xFF, 0xFF, 0xFF);
        public static Color Surface2 => Color.FromArgb(0xF5, 0xF7, 0xFA);
        public static Color Surface3 => Color.FromArgb(0xE8, 0xED, 0xF2);

        // ── Accent ───────────────────────────────────────────────────────────
        public static Color Accent      => Color.FromArgb(0x25, 0x63, 0xEB);
        public static Color AccentHover => Color.FromArgb(0x1D, 0x4E, 0xD8);
        public static Color AccentLight => Color.FromArgb(0xEF, 0xF6, 0xFF);

        // ── Text ─────────────────────────────────────────────────────────────
        public static Color TextPrimary   => Color.FromArgb(0x11, 0x18, 0x27);
        public static Color TextSecondary => Color.FromArgb(0x6B, 0x72, 0x80);
        public static Color TextMuted     => Color.FromArgb(0x9C, 0xA3, 0xAF);
        public static Color HeaderText    => Color.FromArgb(0x37, 0x41, 0x51);

        // ── Borders ──────────────────────────────────────────────────────────
        public static Color Border => Color.FromArgb(0xE5, 0xE7, 0xEB);

        // ── Grid ─────────────────────────────────────────────────────────────
        public static Color RowAlt      => Color.FromArgb(0xF9, 0xFA, 0xFB);
        public static Color RowSelected => Color.FromArgb(0xEF, 0xF6, 0xFF);

        // ── Risk badges ──────────────────────────────────────────────────────
        public static Color LowBg  => Color.FromArgb(0xD1, 0xFA, 0xE5);
        public static Color LowFg  => Color.FromArgb(0x06, 0x5F, 0x46);
        public static Color MedBg  => Color.FromArgb(0xFE, 0xF3, 0xC7);
        public static Color MedFg  => Color.FromArgb(0x92, 0x40, 0x0E);
        public static Color HighBg => Color.FromArgb(0xFE, 0xE2, 0xE2);
        public static Color HighFg => Color.FromArgb(0x99, 0x1B, 0x1B);

        // ── Status dots ──────────────────────────────────────────────────────
        public static Color DotActive   => Color.FromArgb(0x10, 0xB9, 0x81);
        public static Color DotInactive => Color.FromArgb(0xEF, 0x44, 0x44);
        public static Color DotUnknown  => Color.FromArgb(0x9C, 0xA3, 0xAF);
        public static Color DotWarning  => Color.FromArgb(0xF5, 0x9E, 0x0B);

        // ── Typography ───────────────────────────────────────────────────────
        public static Font FontBase    => new("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static Font FontSm      => new("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static Font FontBold    => new("Segoe UI", 9.5f, FontStyle.Bold,    GraphicsUnit.Point);
        public static Font FontSmBold  => new("Segoe UI", 8.5f, FontStyle.Bold,    GraphicsUnit.Point);
        public static Font FontLg      => new("Segoe UI", 12f,  FontStyle.Bold,    GraphicsUnit.Point);
        public static Font FontXl      => new("Segoe UI", 14f,  FontStyle.Bold,    GraphicsUnit.Point);

        // ── Helpers ──────────────────────────────────────────────────────────
        public static (Color Bg, Color Fg) BadgeColors(string risk) => risk?.ToUpper() switch
        {
            "HIGH" or "CRITICAL" => (HighBg, HighFg),
            "MEDIUM"             => (MedBg, MedFg),
            "LOW"                => (LowBg, LowFg),
            _                   => (Surface2, TextSecondary)
        };

        public static Color StatusColor(string status) => status?.ToLower() switch
        {
            "live" or "active" or "online" => DotActive,
            "high" or "critical"           => DotInactive,
            "medium" or "warning"          => DotWarning,
            _                              => DotUnknown
        };
    }
}
