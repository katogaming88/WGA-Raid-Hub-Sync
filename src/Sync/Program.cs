namespace Sync;

static class Program
{
    [STAThread]
    static void Main()
    {
        // One copy at a time: two would upload the same data twice.
        using var single = new Mutex(true, @"Local\WGA-Raid-Hub-Sync", out var first);
        if (!first) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
