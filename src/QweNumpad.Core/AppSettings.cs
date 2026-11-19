using System.Text.Json;
using System.Text.Json.Serialization;

namespace QweNumpad.Core;

public sealed class HotkeyDefinition
{
    /// <summary>Win32 virtual-key code of the trigger key.</summary>
    public int KeyCode { get; set; } = 0x4E; // N

    public bool Control { get; set; } = true;
    public bool Alt { get; set; } = true;
    public bool Shift { get; set; }
    public bool Windows { get; set; }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Control) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        if (Windows) parts.Add("Win");
        parts.Add(((ConsoleKey)KeyCode).ToString());
        return string.Join("+", parts);
    }
}

/// <summary>User-editable configuration, stored next to the learned-field rules.</summary>
public sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public HotkeyDefinition OverrideHotkey { get; set; } = new();

    /// <summary>Extra field-name keywords that should count as numeric.</summary>
    public List<string> ExtraKeywords { get; set; } = new();

    /// <summary>Source virtual-key code -> numpad key. Empty means use <see cref="KeyMap.Default"/>.</summary>
    public Dictionary<int, VirtualKey> CustomMap { get; set; } = new();

    public bool StartWithWindows { get; set; }

    /// <summary>Show a tray balloon the first few times the mode switches, to make it visible.</summary>
    public bool NotifyOnModeChange { get; set; } = true;

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QweNumpad",
        "settings.json");

    public KeyMap BuildKeyMap() =>
        CustomMap.Count == 0 ? KeyMap.Default : KeyMap.FromPairs(CustomMap);

    public static AppSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new AppSettings();

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions)
                   ?? new AppSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Save(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Settings are a convenience; failing to persist them must not crash the app.
        }
    }
}
