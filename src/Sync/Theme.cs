using System.Drawing.Text;
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

    // The site's font, shipped beside the exe (Fonts\, SIL Open Font License). Static Regular and SemiBold
    // files because GDI can't pick weights out of the variable font. Segoe UI, the site's own fallback,
    // if they can't be loaded.
    private static readonly PrivateFontCollection fonts = LoadFonts();
    public static Font Body { get; } = Make("Onest", "Segoe UI", 10f);
    public static Font Strong { get; } = Make("Onest SemiBold", "Segoe UI Semibold", 10f);
    public static Font Title { get; } = Make("Onest SemiBold", "Segoe UI Semibold", 15f);
    public static Font Stat { get; } = Make("Onest SemiBold", "Segoe UI Semibold", 18f);
    public static Font Icons { get; } = new("Segoe MDL2 Assets", 12f);

    /// <summary>The app's logo at the size asked for; the .ico holds 16 to 256 px.</summary>
    public static Icon Logo(Size size)
    {
        using var stream = typeof(Theme).Assembly.GetManifestResourceStream("Sync.ico")!;
        return new Icon(stream, size);
    }

    private static PrivateFontCollection LoadFonts()
    {
        var collection = new PrivateFontCollection();
        foreach (var file in new[] { "Onest-Regular.ttf", "Onest-SemiBold.ttf" })
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Fonts", file);
            if (!File.Exists(path)) continue;
            collection.AddFontFile(path);
            // WinForms labels draw with GDI, which only sees fonts added this way too.
            _ = AddFontResourceEx(path, FrPrivate, IntPtr.Zero);
        }
        return collection;
    }

    private static Font Make(string family, string fallback, float size) =>
        fonts.Families.FirstOrDefault(f => f.Name == family) is { } found ? new(found, size) : new(fallback, size);

    private const int FrPrivate = 0x10;

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern int AddFontResourceEx(string path, int flags, IntPtr reserved);

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
