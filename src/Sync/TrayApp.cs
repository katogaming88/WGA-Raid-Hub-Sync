using Microsoft.Win32;

namespace Sync;

/// <summary>
/// Runs for as long as Sync does: the tray icon, the officer's picks, and the window when it's open.
/// Closing the window leaves this running; Quit is the only way out.
/// </summary>
public sealed class TrayApp : ApplicationContext
{
    public const string SiteUrl = "https://katogaming88.github.io/WGA-Raid-Hub/";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "WGA Raid Hub Sync";

    public Settings Settings { get; } = Settings.Load(Settings.DefaultPath);
    private readonly NotifyIcon tray;
    private MainWindow? window;

    /// <summary>The game folder in use this session; null while the saved one isn't there.</summary>
    public string? Folder { get; private set; }
    private bool warnedSaveFailed;

    /// <summary>A pick changed: the window redraws what it shows.</summary>
    public event Action? Changed;

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
        tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) OpenWindow(); };

        Folder = WowFolders.Normalize(Settings.WowFolder) ?? WowFolders.FindRetail();
        if (Folder is not null && !SameFolder(Folder, Settings.WowFolder))
        {
            // Ticks belong to the folder they were made in.
            Settings.Accounts.Clear();
            Settings.WowFolder = Folder;
            Save();
        }

        // With nothing found, the saved folder and ticks are kept: the drive may just not be connected yet.
        if (Settings.OpenWindowOnStart) OpenWindow();
        else if (Folder is null)
            Note("Couldn't find World of Warcraft. Click this icon to open Sync and choose its folder.");
    }

    // Windows paths ignore letter case: D:\WoW and d:\wow are one folder.
    private static bool SameFolder(string? a, string? b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private void Note(string text, ToolTipIcon icon = ToolTipIcon.Info) =>
        tray.ShowBalloonTip(10_000, "WGA Raid Hub Sync", text, icon);

    public void Save()
    {
        Changed?.Invoke();
        if (Settings.TrySave(Settings.DefaultPath) || warnedSaveFailed) return;
        warnedSaveFailed = true;
        Note("Couldn't save your choices. They work until Sync closes, then need picking again.", ToolTipIcon.Warning);
    }

    /// <summary>What still stands between the officer and a working sync, most important first.</summary>
    internal static List<string> NeedsALook(string? folder, string? savedFolder, int accountsTicked)
    {
        // Linking waits on the site's link page (sync #1); every install shows it until then.
        List<string> items = ["Not linked to Raid Hub yet. Linking opens once the new site is live."];
        if (folder is null)
            items.Add(savedFolder is not null
                ? $"The game folder isn't there: {savedFolder}. Connect its drive, or choose another."
                : "World of Warcraft wasn't found. Choose its folder under Game accounts.");
        else if (accountsTicked == 0)
            items.Add("No game accounts ticked. Tick the ones to sync under Game accounts.");
        return items;
    }

    /// <summary>The one-line status in the tray menu and at the top of Overview.</summary>
    public string Status =>
        Folder is null ? "Game folder not found" : "Not linked to Raid Hub yet";

    /// <summary>The game accounts in the folder, or null when it can't be read.</summary>
    public IReadOnlyList<GameAccount>? Accounts()
    {
        if (Folder is null) return [];
        try { return WowFolders.ListAccounts(Folder); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return null; }
    }

    public void SetAccount(string name, bool ticked)
    {
        Settings.Accounts.Remove(name);
        if (ticked) Settings.Accounts.Add(name);
        Save();
    }

    public void OpenWindow()
    {
        if (window is { IsDisposed: false })
        {
            if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal;
            window.Activate();
            return;
        }
        window = new MainWindow(this);
        window.FormClosed += (_, _) =>
        {
            window = null;
            if (Settings.TrayNoteShown) return;
            Settings.TrayNoteShown = true;
            Save();
            Note("Sync is still running. Open it again from this icon (it may be under the ^ arrow).");
        };
        window.Show();
    }

    private void BuildMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();
        var open = new ToolStripMenuItem("Open Sync", null, (_, _) => OpenWindow());
        open.Font = new Font(open.Font, FontStyle.Bold);
        menu.Items.Add(open);
        menu.Items.Add(new ToolStripMenuItem(Status) { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => ExitThread());
    }

    public void ChooseFolder(IWin32Window owner)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose your World of Warcraft folder",
            UseDescriptionForTitle = true,
            InitialDirectory = Folder ?? "",
        };
        if (dialog.ShowDialog(owner) != DialogResult.OK) return;

        var retail = WowFolders.Normalize(dialog.SelectedPath);
        if (retail is null)
        {
            MessageBox.Show(owner,
                "That isn't a World of Warcraft folder. Choose the folder with Wow.exe in it " +
                "(usually World of Warcraft\\_retail_). Shortcuts and links to other folders aren't accepted.",
                "WGA Raid Hub Sync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!SameFolder(retail, Settings.WowFolder)) Settings.Accounts.Clear();
        Settings.WowFolder = Folder = retail;
        Save();
    }

    /// <summary>Whether Windows starts Sync at sign-in (the current user's Run key).</summary>
    public static bool StartsWithWindows
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(RunValue) is string;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value) key.SetValue(RunValue, $"\"{Application.ExecutablePath}\"");
            else key.DeleteValue(RunValue, throwOnMissingValue: false);
        }
    }

    protected override void ExitThreadCore()
    {
        window?.Dispose();
        tray.Visible = false;
        tray.Dispose();
        base.ExitThreadCore();
    }
}
