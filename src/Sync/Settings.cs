using System.Text.Json;

namespace Sync;

/// <summary>What the officer picked, kept between runs. Never holds the device key.</summary>
public sealed class Settings
{
    public string? WowFolder { get; set; }
    public List<string> Accounts { get; set; } = [];

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WGA Raid Hub Sync", "settings.json");

    public static Settings Load(string path)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new();
            settings.Accounts ??= [];
            return settings;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            // Missing or unreadable: start fresh; the officer just picks again.
            return new();
        }
    }

    /// <summary>False when the file can't be written (disk full, folder blocked).</summary>
    public bool TrySave(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
