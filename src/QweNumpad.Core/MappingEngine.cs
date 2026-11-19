namespace QweNumpad.Core;

/// <summary>
/// The single decision point for every keystroke. Pure and synchronous on purpose:
/// the low-level keyboard hook calls this on the OS input thread, so it must never
/// block, allocate heavily, or touch UI Automation.
/// </summary>
public sealed class MappingEngine
{
    /// <summary>Any of these held means the user wants the real key, not a digit.</summary>
    private const KeyModifiers BypassModifiers =
        KeyModifiers.Shift | KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Windows;

    private readonly KeyMap _map;

    public MappingEngine(KeyMap map) => _map = map;

    public KeyDecision Decide(int virtualKeyCode, KeyModifiers modifiers, bool modeActive)
    {
        if (!modeActive) return KeyDecision.Passthrough;
        if ((modifiers & BypassModifiers) != 0) return KeyDecision.Passthrough;

        return _map.TryGet(virtualKeyCode, out var output)
            ? KeyDecision.Translate(output)
            : KeyDecision.Passthrough;
    }
}
