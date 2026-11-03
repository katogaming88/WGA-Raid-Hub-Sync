using Microsoft.Win32;

namespace Sync;

/// <summary>One Battle.net game account's saved-data folder and the characters on it.</summary>
public sealed record GameAccount(string Name, string Folder, IReadOnlyList<string> Characters);

public static class WowFolders
{
    /// <summary>Finds the retail game folder, or null when it isn't in any usual place.</summary>
    public static string? FindRetail()
    {
        string?[] candidates =
        [
            // Battle.net records the install in Windows' uninstall list; older
            // installs also wrote Blizzard's own key.
            Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\World of Warcraft",
                "InstallLocation", null) as string,
            Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Blizzard Entertainment\World of Warcraft",
                "InstallPath", null) as string,
            @"C:\Program Files (x86)\World of Warcraft",
            @"C:\Program Files\World of Warcraft",
        ];
        return candidates.Select(Normalize).FirstOrDefault(p => p is not null);
    }

    /// <summary>
    /// Turns a folder the officer picked into the retail folder: accepts either
    /// "_retail_" itself or the "World of Warcraft" folder above it. Null when
    /// it isn't a real retail folder.
    /// </summary>
    public static string? Normalize(string? picked)
    {
        if (string.IsNullOrWhiteSpace(picked)) return null;
        var full = Path.GetFullPath(picked.Trim().TrimEnd('\\', '/'));
        if (IsRetail(full)) return full;
        var inside = Path.Combine(full, "_retail_");
        return IsRetail(inside) ? inside : null;
    }

    /// <summary>
    /// A real retail folder has Wow.exe in it, and neither it nor any folder
    /// above it is a link. Sync writes into this folder later, so a link that
    /// points somewhere else is refused rather than followed.
    /// </summary>
    public static bool IsRetail(string folder) =>
        Directory.Exists(folder)
        && File.Exists(Path.Combine(folder, "Wow.exe"))
        && !IsUnderLink(folder);

    /// <summary>
    /// Every game account under WTF\Account with its characters, as
    /// "Name-Realm". Empty when the game has never been logged into.
    /// </summary>
    public static IReadOnlyList<GameAccount> ListAccounts(string retail)
    {
        var root = new DirectoryInfo(Path.Combine(retail, "WTF", "Account"));
        if (!root.Exists) return [];
        return RealFolders(root)
            .Where(account => account.Name != "SavedVariables")
            .Select(account => new GameAccount(
                account.Name,
                account.FullName,
                RealFolders(account)
                    .Where(realm => realm.Name != "SavedVariables")
                    .SelectMany(realm => RealFolders(realm).Select(c => $"{c.Name}-{realm.Name}"))
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<DirectoryInfo> RealFolders(DirectoryInfo parent) =>
        parent.EnumerateDirectories().Where(d => d.LinkTarget is null);

    private static bool IsUnderLink(string folder)
    {
        for (var d = new DirectoryInfo(folder); d is not null; d = d.Parent)
            if (d.LinkTarget is not null) return true;
        return false;
    }
}
