using System.Diagnostics;

namespace Sync;

/// <summary>
/// Sync's window: a sidebar of pages, with Settings at its foot. Built fresh each time it opens,
/// so it always shows the current picks and the current Windows theme.
/// </summary>
public sealed class MainWindow : Form
{
    private static readonly string[] Pages = ["Overview", "Teams", "Game accounts", "Uploads", "Activity"];

    private readonly TrayApp app;
    private readonly Theme t = Theme.Current();
    private readonly FlowLayoutPanel nav = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly Label title = new() { AutoSize = true, Font = Theme.Title };
    private readonly FlowLayoutPanel body = new()
    {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
        AutoScroll = true, Padding = new Padding(24, 8, 24, 24),
    };
    private readonly Button settingsNav;
    /// <summary>"1.0.0", without the commit hash .NET appends.</summary>
    private static string Version => Application.ProductVersion.Split('+')[0];
    private string page = Pages[0];

    public MainWindow(TrayApp app)
    {
        this.app = app;
        settingsNav = NavButton("Settings");
        Text = "WGA Raid Hub Sync";
        Icon = Theme.Logo(SystemInformation.IconSize);
        Font = Theme.Body;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(860, 560);
        MinimumSize = new Size(640, 420);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = t.Panel;
        ForeColor = t.Text;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var sidebar = new Panel { Dock = DockStyle.Fill, BackColor = t.Sidebar, Margin = Padding.Empty, Padding = new Padding(0, 16, 0, 0) };
        sidebar.Controls.Add(nav);
        var brand = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(14, 0, 0, 0), WrapContents = false };
        // Drawn down from 64 px so it stays sharp when Windows scales the window up.
        using (var logo = Theme.Logo(new Size(64, 64)))
            brand.Controls.Add(new PictureBox
            {
                Image = logo.ToBitmap(), SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(24, 24), Margin = new Padding(0, 0, 8, 0),
            });
        brand.Controls.Add(new Label
        {
            Text = "Raid Hub Sync", Font = Theme.Strong, ForeColor = t.Brand,
            AutoSize = true, Margin = new Padding(0, 3, 0, 0),
        });
        sidebar.Controls.Add(brand);
        // Settings sits alone at the foot of the sidebar, apart from the pages about syncing.
        settingsNav.Dock = DockStyle.Bottom;
        sidebar.Controls.Add(settingsNav);
        // Added after Settings so it docks below it, keeping Settings off the window edge.
        sidebar.Controls.Add(new Label
        {
            Text = $"Version {Version}", Font = Theme.Small, ForeColor = t.TextMuted, Dock = DockStyle.Bottom,
            Height = 36, Padding = new Padding(16, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft,
        });
        layout.Controls.Add(sidebar, 0, 0);
        layout.Controls.Add(Rule(vertical: true), 1, 0);

        var header = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(24, 16, 16, 0) };
        title.Dock = DockStyle.Fill; // Docked, so the header's padding lines it up with the page below.
        title.Padding = new Padding(3, 3, 0, 0);
        header.Controls.Add(title);

        var main = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        main.Controls.Add(body);
        main.Controls.Add(header);
        layout.Controls.Add(main, 2, 0);
        Controls.Add(layout);

        foreach (var name in Pages) nav.Controls.Add(NavButton(name));

        // Deferred: a tick's own CheckedChanged triggers this, and Render replaces that checkbox.
        Action changed = () => { if (IsHandleCreated) BeginInvoke(Render); };
        app.Changed += changed;
        FormClosed += (_, _) => app.Changed -= changed;
        HandleCreated += (_, _) => t.StyleTitleBar(Handle);
        Render();
    }

    private void Render()
    {
        SuspendLayout();
        foreach (Button button in nav.Controls) StyleNav(button);
        StyleNav(settingsNav);

        title.Text = page;
        // The rebuilt page gets keyboard focus back on the same checkbox or button.
        var focused = ActiveControl is ButtonBase b && body.Contains(b) ? b.Text : null;
        var old = body.Controls.Cast<Control>().ToList();
        body.Controls.Clear();
        old.ForEach(c => c.Dispose());
        switch (page)
        {
            case "Overview": Overview(); break;
            case "Teams": Line("Your teams show here once Sync is linked to Raid Hub. You'll tick which ones to sync."); break;
            case "Game accounts": GameAccounts(); break;
            case "Uploads": Line("Each upload shows here with when it last ran and whether it worked: the addon's saved data, the loot history and the priority data for the game."); break;
            case "Activity": Line("Nothing yet. Every sync Sync runs is listed here, newest first."); break;
            case "Settings": SettingsPage(); break;
        }
        body.Controls.OfType<ButtonBase>().FirstOrDefault(c => c.Text == focused)?.Focus();
        ResumeLayout();
    }

    // Made once and restyled on each Render, so a click never disposes the button it came from.
    private Button NavButton(string name)
    {
        var button = new Button
        {
            Text = name, TextAlign = ContentAlignment.MiddleLeft, Width = 200, Height = 36,
            ImageAlign = ContentAlignment.MiddleLeft, TextImageRelation = TextImageRelation.ImageBeforeText,
            Margin = Padding.Empty, Padding = new Padding(12, 0, 0, 0),
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = t.NavSelected;
        button.Click += (_, _) => { page = name; Render(); };
        return button;
    }

    private void StyleNav(Button button)
    {
        var selected = button.Text == page;
        button.Font = selected ? Theme.Strong : Theme.Body;
        button.BackColor = selected ? t.NavSelected : t.Sidebar;
        button.ForeColor = selected ? t.Text : t.TextMuted;
        button.AccessibleDescription = selected ? "Current page" : null;
        var old = button.Image;
        button.Image = NavIcon(NavGlyphs[button.Text], button.ForeColor);
        old?.Dispose();
    }

    // Segoe MDL2 Assets ships with Windows 10 and 11: Home, People, Game, Upload, History, Settings.
    private static readonly Dictionary<string, char> NavGlyphs = new()
    {
        ["Overview"] = '', ["Teams"] = '', ["Game accounts"] = '',
        ["Uploads"] = '', ["Activity"] = '', ["Settings"] = '',
    };

    /// <summary>The page's icon in its text color, sized for the screen so it stays sharp when scaled.</summary>
    private Bitmap NavIcon(char glyph, Color color)
    {
        var size = LogicalToDeviceUnits(24);
        var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        // Plain anti-aliasing: ClearType on a see-through bitmap leaves dark fringes.
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe MDL2 Assets", LogicalToDeviceUnits(16), GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        using var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(glyph.ToString(), font, brush, new RectangleF(0, 0, size, size), center);
        return bitmap;
    }

    private void Overview()
    {
        var accounts = app.Accounts();
        var ticked = accounts?.Count(a => app.Settings.Accounts.Contains(a.Name)) ?? 0;

        // The ember Sync now button joins this line with linking (sync #1): there's nothing to sync before it,
        // and a disabled button's text can't be colored to read on these panels.
        Line(app.Status, Theme.Strong, top: 4);
        body.Controls.Add(new Panel { Height = 12, Width = 1, Margin = Padding.Empty });
        body.Controls.Add(Rule());

        var stats = new TableLayoutPanel { AutoSize = true, ColumnCount = 3, Margin = new Padding(0, 16, 0, 16) };
        Stat(stats, accounts is null ? "?" : $"{ticked} of {accounts.Count}", "game accounts ticked");
        Stat(stats, "None", "teams synced");
        Stat(stats, "Never", "last upload");
        body.Controls.Add(stats);
        body.Controls.Add(Rule());

        Line("Needs a look", Theme.Strong, top: 16);
        foreach (var item in TrayApp.NeedsALook(app.Folder, app.Settings.WowFolder, ticked))
            Line(item, color: t.EmberText);
    }

    private void Stat(TableLayoutPanel stats, string value, string label)
    {
        var cell = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Margin = new Padding(0, 0, 48, 0) };
        cell.Controls.Add(new Label { Text = value, Font = Theme.Stat, AutoSize = true, Margin = Padding.Empty });
        cell.Controls.Add(new Label { Text = label, ForeColor = t.TextMuted, AutoSize = true, Margin = Padding.Empty });
        stats.Controls.Add(cell);
    }

    private void GameAccounts()
    {
        Line(app.Folder is not null ? $"Game folder: {app.Folder}"
            : app.Settings.WowFolder is not null ? $"Game folder not available: {app.Settings.WowFolder}"
            : "World of Warcraft folder not found");
        var choose = new Button { Text = "Choose game folder...", AutoSize = true, Margin = new Padding(0, 8, 0, 16) };
        Plain(choose);
        choose.Click += (_, _) => app.ChooseFolder(this);
        body.Controls.Add(choose);
        if (app.Folder is null) return;

        body.Controls.Add(Rule());
        var accounts = app.Accounts();
        Line(accounts is null ? "Can't read this folder's game accounts."
            : accounts.Count == 0 ? "No game accounts yet: log into the game once."
            : "Sync these game accounts:", Theme.Strong, top: 16);
        foreach (var account in accounts ?? [])
        {
            var box = new CheckBox
            {
                Text = Describe(account), AutoSize = true, Margin = new Padding(0, 6, 0, 0),
                Checked = app.Settings.Accounts.Contains(account.Name),
            };
            box.CheckedChanged += (_, _) => app.SetAccount(account.Name, box.Checked);
            body.Controls.Add(box);
        }
    }

    /// <summary>"ACCOUNT1  (Katorri-Stormrage, Alt-Stormrage, +3 more)", so it's clear which account is which.</summary>
    internal static string Describe(GameAccount account)
    {
        const int shown = 3;
        var names = account.Characters.Take(shown).ToList();
        if (account.Characters.Count > shown) names.Add($"+{account.Characters.Count - shown} more");
        return names.Count == 0 ? account.Name : $"{account.Name}  ({string.Join(", ", names)})";
    }

    // The game folder is chosen on Game accounts. Check for updates and What's new come with the
    // installer (sync #5); Unlink with linking (sync #1).
    private void SettingsPage()
    {
        Option("Start with Windows", TrayApp.StartsWithWindows, on => TrayApp.StartsWithWindows = on);
        Option("Start minimized to the tray", app.Settings.StartInTray, on =>
        {
            app.Settings.StartInTray = on;
            app.Save();
        });
        Line("Sync starts with only its tray icon. Click the icon to open this window.", color: t.TextMuted);
        body.Controls.Add(new Panel { Height = 12, Width = 1, Margin = Padding.Empty });
        body.Controls.Add(Rule());

        LinkButton("Open Sync's data folder", () =>
        {
            var folder = Path.GetDirectoryName(Settings.DefaultPath)!;
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }, top: 16);
        LinkButton("Open Raid Hub", () => Process.Start(new ProcessStartInfo(TrayApp.SiteUrl) { UseShellExecute = true }));
    }

    private void Option(string text, bool on, Action<bool> set)
    {
        var box = new CheckBox { Text = text, Checked = on, AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
        box.CheckedChanged += (_, _) => set(box.Checked);
        body.Controls.Add(box);
    }

    private void LinkButton(string text, Action click, int top = 0)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(0, top, 0, 8) };
        Plain(button);
        button.Click += (_, _) => click();
        body.Controls.Add(button);
    }

    private void Line(string text, Font? font = null, Color? color = null, int top = 0) =>
        body.Controls.Add(new Label
        {
            Text = text, Font = font ?? Theme.Body, ForeColor = color ?? t.Text,
            AutoSize = true, MaximumSize = new Size(560, 0), Margin = new Padding(0, top, 0, 4),
        });

    /// <summary>A hairline rule: the site separates with lines, not boxes.</summary>
    private Control Rule(bool vertical = false) => vertical
        ? new Panel { Dock = DockStyle.Fill, BackColor = t.Border, Margin = Padding.Empty }
        : new Panel { Height = 1, Width = 560, BackColor = t.Border, Margin = Padding.Empty };

    private void Plain(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = t.Panel;
        button.ForeColor = t.Text;
        button.Cursor = Cursors.Hand;
        button.FlatAppearance.BorderColor = t.BorderStrong;
        button.FlatAppearance.MouseOverBackColor = t.NavSelected;
    }
}
