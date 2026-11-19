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

    /// <summary>
    /// Stable, case-insensitive key used for persistence and lookup.
    /// </summary>
    /// <remarks>
    /// Digit runs in the label become <c>#</c>: labels often carry the field's current value
    /// or a row number ("Tutar: 150 TL", "Satır 3"), and if those were part of the key a
    /// taught rule would stop matching as soon as the value changed.
    /// The automation id keeps its digits, because Win32 dialogs expose control ids there
    /// ("1001", "1002") and those are exactly what distinguishes two fields.
    /// </remarks>
    public string Key =>
        string.Join('|',
            ProcessName.Trim().ToLowerInvariant(),
            ControlType.Trim().ToLowerInvariant(),
            AutomationId.Trim().ToLowerInvariant(),
            NormalizeLabel(Name));

    private static readonly System.Text.RegularExpressions.Regex DigitRuns =
        new(@"\d+", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex WhitespaceRuns =
        new(@"\s+", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static string NormalizeLabel(string label)
    {
        var lowered = label.Trim().ToLowerInvariant();
        var withoutNumbers = DigitRuns.Replace(lowered, "#");
        return WhitespaceRuns.Replace(withoutNumbers, " ");
    }

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
