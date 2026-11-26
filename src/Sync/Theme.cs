using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Sync;

/// <summary>
/// The site's colors (app/src/styles/tokens.css on the site repo), light or dark to match Windows.
/// Read when the window opens; a theme change shows the next time it opens.
/// </summary>
public sealed record Theme(
    bool Dark, Color Page, Color Sidebar, Color Panel, Color NavSelected, Color Border, Color BorderStrong,
    Color Text, Color TextMuted, Color Brand, Color Button, Color ButtonText, Color EmberText)
{
    public static readonly Theme DarkTheme = new(true,
        Hex("#0b0e13"), Hex("#0f131a"), Hex("#131820"), Hex("#1a2029"), Hex("#212833"), Hex("#2c3441"),
        Hex("#e6ebf2"), Hex("#9da8b8"), Hex("#5b9cf5"), Hex("#f2703f"), Hex("#1c0b04"), Hex("#f2703f"));

    public static readonly Theme LightTheme = new(false,
        Hex("#e4e9f0"), Hex("#dae1ea"), Hex("#f6f8fb"), Hex("#f6f8fb"), Hex("#cfd7e2"), Hex("#bcc7d5"),
        Hex("#0f1623"), Hex("#46505f"), Hex("#255fc0"), Hex("#c24a14"), Hex("#ffffff"), Hex("#a63a0a"));

    public static Theme Current()
    {
        // Missing on older Windows: dark is the site's default too.
        var light = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme", 0) is int value && value == 1;
        return light ? LightTheme : DarkTheme;
    }

    private static Color Hex(string hex) => ColorTranslator.FromHtml(hex);

    // ponytail: Segoe UI, the site's own fallback for Onest. Embedding Onest needs a TTF in the repo.
    public static Font Body { get; } = new("Segoe UI", 10f);
    public static Font Strong { get; } = new("Segoe UI Semibold", 10f);
    public static Font Title { get; } = new("Segoe UI Semibold", 15f);
    public static Font Stat { get; } = new("Segoe UI Semibold", 18f);
    public static Font Icons { get; } = new("Segoe MDL2 Assets", 12f);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>Dark title bar to match a dark window (Windows 10 2004 and later; ignored before).</summary>
    public void StyleTitleBar(IntPtr hwnd)
    {
        const int useImmersiveDarkMode = 20;
        var on = Dark ? 1 : 0;
        _ = DwmSetWindowAttribute(hwnd, useImmersiveDarkMode, ref on, sizeof(int));
    }

    /// <summary>Menus in the window's colors, not the stock light grey.</summary>
    public ToolStripRenderer MenuRenderer() => new ToolStripProfessionalRenderer(new MenuColors(this));

    private sealed class MenuColors(Theme t) : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => t.Panel;
        public override Color ImageMarginGradientBegin => t.Panel;
        public override Color ImageMarginGradientMiddle => t.Panel;
        public override Color ImageMarginGradientEnd => t.Panel;
        public override Color MenuBorder => t.BorderStrong;
        public override Color MenuItemBorder => t.NavSelected;
        public override Color MenuItemSelected => t.NavSelected;
        public override Color SeparatorDark => t.Border;
        public override Color SeparatorLight => t.Border;
        public override Color CheckBackground => t.NavSelected;
        public override Color CheckSelectedBackground => t.NavSelected;
        public override Color CheckPressedBackground => t.NavSelected;
    }
}
