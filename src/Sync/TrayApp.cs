namespace Sync;

/// <summary>The tray icon and its right-click menu. There is no main window.</summary>
public sealed class TrayApp : ApplicationContext
{
    private readonly Settings settings = Settings.Load(Settings.DefaultPath);
    private readonly NotifyIcon tray;

    /// <summary>The game folder in use this session; null while the saved one isn't there.</summary>
    private string? folder;
    private bool warnedSaveFailed;

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

        folder = WowFolders.Normalize(settings.WowFolder) ?? WowFolders.FindRetail();
        if (folder is not null && folder != settings.WowFolder)
        {
            // Ticks belong to the folder they were made in.
            settings.Accounts.Clear();
            settings.WowFolder = folder;
            Save();
        }
        // With nothing found, the saved folder and ticks are kept: the drive may just not be connected yet.
        if (folder is null)
            tray.ShowBalloonTip(10_000, "WGA Raid Hub Sync",
                "Couldn't find World of Warcraft. Right-click this icon to choose its folder.", ToolTipIcon.Info);
    }

    private void Save()
    {
        if (settings.TrySave(Settings.DefaultPath) || warnedSaveFailed) return;
        warnedSaveFailed = true;
        tray.ShowBalloonTip(10_000, "WGA Raid Hub Sync",
            "Couldn't save your choices. They work until Sync closes, then need picking again.", ToolTipIcon.Warning);
    }

    private void BuildMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();
        menu.Items.Add(new ToolStripMenuItem("Not linked to Raid Hub yet") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add(new ToolStripMenuItem(
            folder is not null ? $"Game folder: {folder}"
            : settings.WowFolder is not null ? $"Game folder not available: {settings.WowFolder}"
            : "World of Warcraft folder not found") { Enabled = false });
        menu.Items.Add("Choose game folder...", null, (_, _) => ChooseFolder());

        if (folder is not null)
        {
            menu.Items.Add(new ToolStripSeparator());
            IReadOnlyList<GameAccount> accounts;
            string heading;
            try
            {
                accounts = WowFolders.ListAccounts(folder);
                heading = accounts.Count == 0
                    ? "No game accounts yet: log into the game once"
                    : "Sync these game accounts:";
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Keep the menu usable so the officer can choose another folder.
                accounts = [];
                heading = "Can't read this folder's game accounts";
            }
            menu.Items.Add(new ToolStripMenuItem(heading) { Enabled = false });
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
            Save();
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
            InitialDirectory = folder ?? "",
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
        settings.WowFolder = folder = retail;
        Save();
    }

    protected override void ExitThreadCore()
    {
        tray.Visible = false;
        tray.Dispose();
        base.ExitThreadCore();
    }
}
