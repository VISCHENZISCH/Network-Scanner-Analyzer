using System.Drawing;

namespace NetScanAnalyzer.UI.Styles
{
    public static class ThemeManager
    {
        public static Color BackgroundDark = Color.FromArgb(24, 24, 27);
        public static Color SurfaceDark = Color.FromArgb(39, 39, 42);
        public static Color AccentColor = Color.FromArgb(99, 102, 241); // Indigo
        public static Color TextPrimary = Color.FromArgb(244, 244, 245);
        public static Color TextSecondary = Color.FromArgb(161, 161, 170);
        public static Color Success = Color.FromArgb(34, 197, 94);
        public static Color Danger = Color.FromArgb(239, 68, 68);
        public static Color Warning = Color.FromArgb(245, 158, 11);

        public static void ApplyDarkTheme(Control control)
        {
            control.BackColor = BackgroundDark;
            control.ForeColor = TextPrimary;

            foreach (Control child in control.Controls)
            {
                if (child is Button btn)
                {
                    btn.BackColor = SurfaceDark;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = AccentColor;
                    btn.ForeColor = TextPrimary;
                }
                else if (child is DataGridView dgv)
                {
                    dgv.BackgroundColor = SurfaceDark;
                    dgv.DefaultCellStyle.BackColor = SurfaceDark;
                    dgv.DefaultCellStyle.ForeColor = TextPrimary;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = BackgroundDark;
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
                    dgv.EnableHeadersVisualStyles = false;
                }
                else if (child is TextBox tb)
                {
                    tb.BackColor = SurfaceDark;
                    tb.ForeColor = TextPrimary;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                }
                
                if (child.HasChildren) ApplyDarkTheme(child);
            }
        }
    }
}
