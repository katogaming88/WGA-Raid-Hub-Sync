namespace Sync;

/// <summary>The tray icon and its right-click menu. There is no main window.</summary>
public sealed class TrayApp : ApplicationContext
{
    private readonly Settings settings = Settings.Load(Settings.DefaultPath);
    private readonly NotifyIcon tray;

    public TrayApp()
    {
        // ponytail: stock icon until the logo is drawn (logo directions are on the site repo).
        tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "WGA Raid Hub Sync",
            ContextMenuStrip = new ContextMenuStrip(),
            Visible = true,
        };
        tray.ContextMenuStrip.Opening += (_, e) =>
        {
            BuildMenu(tray.ContextMenuStrip);
            e.Cancel = false;
        };

        settings.WowFolder = WowFolders.Normalize(settings.WowFolder) ?? WowFolders.FindRetail();
        settings.Save(Settings.DefaultPath);
        if (settings.WowFolder is null)
            tray.ShowBalloonTip(10_000, "WGA Raid Hub Sync",
                "Couldn't find World of Warcraft. Right-click this icon to choose its folder.", ToolTipIcon.Info);
    }

    private void BuildMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();
        menu.Items.Add(new ToolStripMenuItem("Not linked to Raid Hub yet") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add(new ToolStripMenuItem(settings.WowFolder is null
            ? "World of Warcraft folder not found"
            : $"Game folder: {settings.WowFolder}") { Enabled = false });
        menu.Items.Add("Choose game folder...", null, (_, _) => ChooseFolder());

        if (settings.WowFolder is not null)
        {
            menu.Items.Add(new ToolStripSeparator());
            var accounts = WowFolders.ListAccounts(settings.WowFolder);
            menu.Items.Add(new ToolStripMenuItem(accounts.Count == 0
                ? "No game accounts yet: log into the game once"
                : "Sync these game accounts:") { Enabled = false });
            foreach (var account in accounts)
                menu.Items.Add(AccountItem(account));
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => ExitThread());
    }

    private ToolStripMenuItem AccountItem(GameAccount account)
    {
        var item = new ToolStripMenuItem(Describe(account))
        {
            Checked = settings.Accounts.Contains(account.Name),
            CheckOnClick = true,
        };
        item.CheckedChanged += (_, _) =>
        {
            settings.Accounts.Remove(account.Name);
            if (item.Checked) settings.Accounts.Add(account.Name);
            settings.Save(Settings.DefaultPath);
        };
        return item;
    }

    /// <summary>"ACCOUNT1  (Katorri-Stormrage, Alt-Stormrage, +3 more)", so it's clear which account is which.</summary>
    internal static string Describe(GameAccount account)
    {
        const int shown = 3;
        var names = account.Characters.Take(shown).ToList();
        if (account.Characters.Count > shown) names.Add($"+{account.Characters.Count - shown} more");
        return names.Count == 0 ? account.Name : $"{account.Name}  ({string.Join(", ", names)})";
    }

    private void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose your World of Warcraft folder",
            UseDescriptionForTitle = true,
            InitialDirectory = settings.WowFolder ?? "",
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        var retail = WowFolders.Normalize(dialog.SelectedPath);
        if (retail is null)
        {
            MessageBox.Show(
                "That isn't a World of Warcraft folder. Choose the folder with Wow.exe in it " +
                "(usually World of Warcraft\\_retail_). Shortcuts and links to other folders aren't accepted.",
                "WGA Raid Hub Sync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (retail != settings.WowFolder) settings.Accounts.Clear();
        settings.WowFolder = retail;
        settings.Save(Settings.DefaultPath);
    }

    protected override void ExitThreadCore()
    {
        tray.Visible = false;
        tray.Dispose();
        base.ExitThreadCore();
    }
}
