using System.Text.Json;
using System.Text.Json.Serialization;

namespace QweNumpad.Core;

public sealed record LearnedField(string Key, string Description, bool Numeric);

/// <summary>
/// Remembers the user's corrections per field, so a wrong guess only has to be corrected once.
/// Stores field metadata only — process name, control type, automation id, label. Never keystrokes.
/// </summary>
public sealed class LearnedFieldStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _path;
    private readonly Dictionary<string, LearnedField> _entries = new(StringComparer.Ordinal);

    public LearnedFieldStore(string path)
    {
        _path = path;
        Load();
    }

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QweNumpad",
        "learned.json");

    public IReadOnlyCollection<LearnedField> Entries => _entries.Values;

    /// <summary>true = always remap here, false = never remap here, null = no rule yet.</summary>
    public bool? Lookup(FieldSignature signature) =>
        _entries.TryGetValue(signature.Key, out var entry) ? entry.Numeric : null;

    public void Remember(FieldSignature signature, bool numeric)
    {
        _entries[signature.Key] = new LearnedField(signature.Key, signature.ToString(), numeric);
        Save();
    }

    public void Forget(FieldSignature signature)
    {
        if (_entries.Remove(signature.Key)) Save();
    }

    public void ForgetKey(string key)
    {
        if (_entries.Remove(key)) Save();
    }

    public void Clear()
    {
        _entries.Clear();
        Save();
    }

    public void Load()
    {
        _entries.Clear();

        try
        {
            if (!File.Exists(_path)) return;

            var json = File.ReadAllText(_path);
            var loaded = JsonSerializer.Deserialize<List<LearnedField>>(json, JsonOptions);
            if (loaded is null) return;

            foreach (var entry in loaded)
                if (!string.IsNullOrEmpty(entry.Key)) _entries[entry.Key] = entry;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // A corrupt or unreadable store must not stop the app from running.
            _entries.Clear();
        }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(_path, JsonSerializer.Serialize(_entries.Values.ToList(), JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Losing the learned rules is survivable; crashing the hook is not.
        }
    }
}
