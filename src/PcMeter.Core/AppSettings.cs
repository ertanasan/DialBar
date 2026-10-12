using System.Text.Json;
using System.Text.Json.Serialization;

namespace PcMeter.Core;

public sealed class AppSettings
{
    public int? Left { get; set; }
    public int? Top { get; set; }
    public bool AlwaysOnTop { get; set; } = true;
    public bool Vertical { get; set; }
    public double IntervalSeconds { get; set; } = 1;
    public List<MetricKind> HiddenGauges { get; set; } = [];
}

/// <summary>Reads and writes settings as JSON in the per-user app-data folder.</summary>
public static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PcMeter", "settings.json");

    /// <summary>Returns defaults when the file is missing or corrupt.</summary>
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options);
                if (settings is not null)
                {
                    settings.IntervalSeconds = Math.Clamp(settings.IntervalSeconds, 0.5, 10);
                    return settings;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // fall through to defaults
        }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            // Write to a temp file first so a crash mid-write can't corrupt the existing settings.
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
            File.Move(temp, FilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Settings are a convenience; never crash because they can't be saved.
        }
    }
}
