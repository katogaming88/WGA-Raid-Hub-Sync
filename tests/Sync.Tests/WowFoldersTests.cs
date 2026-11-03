namespace Sync.Tests;

public sealed class WowFoldersTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("sync-tests-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string MakeRetail()
    {
        var retail = Path.Combine(root, "World of Warcraft", "_retail_");
        Directory.CreateDirectory(retail);
        File.WriteAllText(Path.Combine(retail, "Wow.exe"), "");
        return retail;
    }

    [Fact]
    public void AcceptsRetailOrTheFolderAboveIt()
    {
        var retail = MakeRetail();
        Assert.Equal(retail, WowFolders.Normalize(retail));
        Assert.Equal(retail, WowFolders.Normalize(Path.GetDirectoryName(retail) + "\\"));
    }

    [Fact]
    public void RefusesFoldersWithoutWowExe()
    {
        Assert.Null(WowFolders.Normalize(root));
        Assert.Null(WowFolders.Normalize(""));
        Assert.Null(WowFolders.Normalize(Path.Combine(root, "missing")));
    }

    [Fact]
    public void RefusesALinkToTheGameFolder()
    {
        var retail = MakeRetail();
        var link = Path.Combine(root, "link");
        // Directory symlinks need admin or Developer Mode; CI runners have it.
        try { Directory.CreateSymbolicLink(link, retail); }
        catch (IOException) { return; }
        catch (UnauthorizedAccessException) { return; }

        Assert.Null(WowFolders.Normalize(link));
    }

    [Fact]
    public void ListsAccountsWithTheirCharacters()
    {
        var retail = MakeRetail();
        var account = Path.Combine(retail, "WTF", "Account", "KATO#1");
        Directory.CreateDirectory(Path.Combine(account, "SavedVariables"));
        Directory.CreateDirectory(Path.Combine(account, "Stormrage", "Katorri"));
        Directory.CreateDirectory(Path.Combine(account, "Stormrage", "Alt"));
        Directory.CreateDirectory(Path.Combine(account, "Area 52", "Bank"));
        Directory.CreateDirectory(Path.Combine(retail, "WTF", "Account", "EMPTY"));
        // Account-wide saved data sits beside the accounts; it isn't one.
        Directory.CreateDirectory(Path.Combine(retail, "WTF", "Account", "SavedVariables"));

        var accounts = WowFolders.ListAccounts(retail);

        Assert.Equal(["EMPTY", "KATO#1"], accounts.Select(a => a.Name));
        Assert.Empty(accounts[0].Characters);
        Assert.Equal(["Alt-Stormrage", "Bank-Area 52", "Katorri-Stormrage"], accounts[1].Characters);
    }

    [Fact]
    public void NoAccountsBeforeFirstLogin() => Assert.Empty(WowFolders.ListAccounts(MakeRetail()));

    [Fact]
    public void DescribeShowsThreeCharactersThenACount()
    {
        var account = new GameAccount("KATO#1", "", ["A-R", "B-R", "C-R", "D-R", "E-R"]);
        Assert.Equal("KATO#1  (A-R, B-R, C-R, +2 more)", TrayApp.Describe(account));
        Assert.Equal("EMPTY", TrayApp.Describe(new GameAccount("EMPTY", "", [])));
    }

    [Fact]
    public void SettingsRoundTripAndSurviveABadFile()
    {
        var path = Path.Combine(root, "s", "settings.json");
        new Settings { WowFolder = @"C:\x", Accounts = ["KATO#1"] }.TrySave(path);
        var loaded = Settings.Load(path);
        Assert.Equal(@"C:\x", loaded.WowFolder);
        Assert.Equal(["KATO#1"], loaded.Accounts);

        File.WriteAllText(path, """{ "WowFolder": null, "Accounts": null }""");
        Assert.Empty(Settings.Load(path).Accounts);

        File.WriteAllText(path, "{ not json");
        Assert.Null(Settings.Load(path).WowFolder);
    }

    [Fact]
    public void SaveReportsFailureInsteadOfThrowing()
    {
        // A file where the settings folder should be: the write can't happen.
        var blocker = Path.Combine(root, "blocked");
        File.WriteAllText(blocker, "");
        Assert.False(new Settings().TrySave(Path.Combine(blocker, "settings.json")));
    }

    [Fact]
    public void RefusesAMalformedPath() => Assert.Null(WowFolders.Normalize("C:\\bad\0path"));

    [Fact]
    public void ListsNothingThroughALinkedWtfFolder()
    {
        var retail = MakeRetail();
        Directory.CreateDirectory(Path.Combine(root, "elsewhere", "Account", "OTHER"));
        // Directory symlinks need admin or Developer Mode; CI runners have it.
        try { Directory.CreateSymbolicLink(Path.Combine(retail, "WTF"), Path.Combine(root, "elsewhere")); }
        catch (IOException) { return; }
        catch (UnauthorizedAccessException) { return; }

        Assert.Empty(WowFolders.ListAccounts(retail));
    }
}
