namespace QweNumpad.Core;

/// <summary>
/// Identifies a focusable field well enough to remember a user's override for it.
/// Deliberately contains only structural metadata — never the text typed into the field.
/// </summary>
public readonly record struct FieldSignature(
    string ProcessName,
    string ControlType,
    string AutomationId,
    string Name)
{
    public static FieldSignature Create(
        string? processName,
        string? controlType,
        string? automationId,
        string? name) =>
        new(processName ?? "", controlType ?? "", automationId ?? "", name ?? "");

    /// <summary>Stable, case-insensitive key used for persistence and lookup.</summary>
    public string Key =>
        string.Join('|',
            ProcessName.Trim().ToLowerInvariant(),
            ControlType.Trim().ToLowerInvariant(),
            AutomationId.Trim().ToLowerInvariant(),
            Name.Trim().ToLowerInvariant());

    public override string ToString() =>
        $"{ProcessName} / {ControlType} / {(string.IsNullOrEmpty(AutomationId) ? "-" : AutomationId)} / {(string.IsNullOrEmpty(Name) ? "-" : Name)}";
}

/// <summary>
/// Everything the focus watcher managed to learn about the currently focused field.
/// Flags come from UI Automation patterns and Win32 window styles; the classifier
/// turns them into a yes/no verdict.
/// </summary>
public sealed record FieldInfo(
    FieldSignature Signature,
    bool HasRangeValuePattern,
    bool IsSpinnerControlType,
    bool HasEsNumberStyle,
    string? HelpText = null,
    string? Placeholder = null,
    bool IsPasswordField = false,
    bool IsReadOnly = false);
