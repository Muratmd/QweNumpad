using QweNumpad.Core;

namespace QweNumpad.App.Input;

/// <summary>
/// Turns raw hook callbacks into actions: fires the override hotkey, and translates
/// mapped keys while keeping key-down and key-up paired.
/// </summary>
/// <remarks>
/// Pairing matters: if a key is translated on the way down, its key-up must also be
/// swallowed and replaced, even if the mode switched off in between. Otherwise the
/// original letter's key-up leaks into the app and keys appear stuck.
/// </remarks>
internal sealed class KeystrokeRouter
{
    private readonly ModeState _state;
    private readonly Dictionary<int, VirtualKey> _translatedDown = new();

    private MappingEngine _engine;
    private HotkeyDefinition _hotkey;
    private int _swallowedHotkeyKey = -1;

    public KeystrokeRouter(ModeState state, MappingEngine engine, HotkeyDefinition hotkey)
    {
        _state = state;
        _engine = engine;
        _hotkey = hotkey;
    }

    public event EventHandler? OverrideHotkeyPressed;

    public void Reconfigure(MappingEngine engine, HotkeyDefinition hotkey)
    {
        _engine = engine;
        _hotkey = hotkey;
    }

    /// <summary>Called from the keyboard hook. Returns true to swallow the key.</summary>
    public bool Handle(int virtualKeyCode, bool isKeyDown, KeyModifiers modifiers)
    {
        if (isKeyDown)
        {
            if (MatchesHotkey(virtualKeyCode, modifiers))
            {
                _swallowedHotkeyKey = virtualKeyCode;
                OverrideHotkeyPressed?.Invoke(this, EventArgs.Empty);
                return true;
            }

            var decision = _engine.Decide(virtualKeyCode, modifiers, _state.IsActive);
            if (decision.Action != KeyAction.Translate) return false;

            _translatedDown[virtualKeyCode] = decision.Output;
            KeyOutputSender.Send(decision.Output, keyUp: false);
            return true;
        }

        if (virtualKeyCode == _swallowedHotkeyKey)
        {
            _swallowedHotkeyKey = -1;
            return true;
        }

        if (!_translatedDown.Remove(virtualKeyCode, out var output)) return false;

        KeyOutputSender.Send(output, keyUp: true);
        return true;
    }

    private bool MatchesHotkey(int virtualKeyCode, KeyModifiers modifiers)
    {
        if (virtualKeyCode != _hotkey.KeyCode) return false;

        var required = KeyModifiers.None;
        if (_hotkey.Control) required |= KeyModifiers.Control;
        if (_hotkey.Alt) required |= KeyModifiers.Alt;
        if (_hotkey.Shift) required |= KeyModifiers.Shift;
        if (_hotkey.Windows) required |= KeyModifiers.Windows;

        // Exact match, so Ctrl+Alt+N does not also fire on Ctrl+Alt+Shift+N.
        return modifiers == required;
    }
}
