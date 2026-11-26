using System.Diagnostics;

namespace Sync;

/// <summary>
/// Sync's window: a sidebar of pages and a Settings button. Built fresh each time it opens,
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
    private string page = Pages[0];

    public MainWindow(TrayApp app)
    {
        this.app = app;
        Text = "WGA Raid Hub Sync";
        Icon = SystemIcons.Application;
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
        sidebar.Controls.Add(new Label
        {
            Text = "Raid Hub Sync", Font = Theme.Strong, ForeColor = t.Brand,
            Dock = DockStyle.Top, Height = 40, Padding = new Padding(16, 0, 0, 0),
        });
        layout.Controls.Add(sidebar, 0, 0);
        layout.Controls.Add(Rule(vertical: true), 1, 0);

        var settings = new Button
        {
            Text = "", Font = Theme.Icons, AccessibleName = "Settings",
            Size = new Size(40, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        Plain(settings, border: false);
        new ToolTip().SetToolTip(settings, "Settings");
        settings.Click += (_, _) => SettingsMenu().Show(settings, new Point(settings.Width, settings.Height), ToolStripDropDownDirection.BelowLeft);

        var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = 64, ColumnCount = 2, Padding = new Padding(24, 16, 16, 0) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(title, 0, 0);
        header.Controls.Add(settings, 1, 0);

        var main = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        main.Controls.Add(body);
        main.Controls.Add(header);
        layout.Controls.Add(main, 2, 0);
        Controls.Add(layout);

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
        nav.Controls.Clear();
        foreach (var name in Pages) nav.Controls.Add(NavButton(name));

        title.Text = page;
        body.Controls.Clear();
        switch (page)
        {
            case "Overview": Overview(); break;
            case "Teams": Line("Your teams show here once Sync is linked to Raid Hub. You'll tick which ones to sync."); break;
            case "Game accounts": GameAccounts(); break;
            case "Uploads": Line("Each upload shows here with when it last ran and whether it worked: the addon's saved data, the loot history and the priority data for the game."); break;
            case "Activity": Line("Nothing yet. Every sync Sync runs is listed here, newest first."); break;
        }
        ResumeLayout();
    }

    private Button NavButton(string name)
    {
        var selected = name == page;
        var button = new Button
        {
            Text = name, TextAlign = ContentAlignment.MiddleLeft, Width = 200, Height = 36,
            Margin = Padding.Empty, Padding = new Padding(12, 0, 0, 0),
            Font = selected ? Theme.Strong : Theme.Body,
            BackColor = selected ? t.NavSelected : t.Sidebar,
            ForeColor = selected ? t.Text : t.TextMuted,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            AccessibleDescription = selected ? "Current page" : null,
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = t.NavSelected;
        button.Click += (_, _) => { page = name; Render(); };
        return button;
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

    private ContextMenuStrip SettingsMenu()
    {
        var menu = new ContextMenuStrip { Renderer = t.MenuRenderer(), ForeColor = t.Text, Font = Theme.Body };
        menu.Items.Add(Toggle("Start with Windows", TrayApp.StartsWithWindows, on => TrayApp.StartsWithWindows = on));
        menu.Items.Add(Toggle("Open this window when Sync starts", app.Settings.OpenWindowOnStart, on =>
        {
            app.Settings.OpenWindowOnStart = on;
            app.Save();
        }));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Choose game folder...", null, (_, _) => app.ChooseFolder(this));
        menu.Items.Add("Open Sync's data folder", null, (_, _) =>
        {
            var folder = Path.GetDirectoryName(Settings.DefaultPath)!;
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        });
        menu.Items.Add("Open Raid Hub", null, (_, _) => Process.Start(new ProcessStartInfo(TrayApp.SiteUrl) { UseShellExecute = true }));
        menu.Items.Add(new ToolStripSeparator());
        // Check for updates and What's new come with the installer (sync #5); Unlink with linking (sync #1).
        menu.Items.Add(new ToolStripMenuItem($"Version {Application.ProductVersion.Split('+')[0]}") { Enabled = false });
        foreach (ToolStripItem item in menu.Items) item.ForeColor = item.Enabled ? t.Text : t.TextMuted;
        return menu;
    }

    private static ToolStripMenuItem Toggle(string text, bool on, Action<bool> set)
    {
        var item = new ToolStripMenuItem(text) { Checked = on, CheckOnClick = true };
        item.CheckedChanged += (_, _) => set(item.Checked);
        return item;
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

    private void Plain(Button button, bool border = true)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = t.Panel;
        button.ForeColor = t.Text;
        button.Cursor = Cursors.Hand;
        // A borderless flat button still draws its border when focused, so give it the panel's color.
        button.FlatAppearance.BorderColor = border ? t.BorderStrong : t.Panel;
        button.FlatAppearance.BorderSize = border ? 1 : 0;
        button.FlatAppearance.MouseOverBackColor = t.NavSelected;
    }
}
