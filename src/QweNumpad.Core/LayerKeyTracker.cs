namespace QweNumpad.Core;

/// <summary>What the hook should do with a layer-key event.</summary>
/// <param name="Swallow">Hide the event from Windows, so the key's own function (Caps Lock) never fires.</param>
/// <param name="Tapped">The press was a tap: short, and no other key went down during it.</param>
public readonly record struct LayerKeyResult(bool Swallow, bool Tapped);

/// <summary>
/// Tells a hold of the layer key (digits while held) from a tap (toggle the numpad lock).
/// </summary>
/// <remarks>
/// Pure and single-threaded: the keyboard hook feeds it, and the caller passes the time in,
/// so the tests need no clock. <c>Shift</c> + layer key is passed through untouched to keep
/// a way to reach the real Caps Lock; its key-up follows the key-down's decision so Windows
/// never sees an unpaired press.
/// </remarks>
public sealed class LayerKeyTracker
{
    /// <summary>A press released within this many milliseconds, with no other key in between, is a tap.</summary>
    public const long TapTimeoutMs = 300;

    private enum Phase { Idle, Held, PassingThrough }

    private Phase _phase = Phase.Idle;
    private long _downAtMs;
    private bool _usedAsLayer;

    public bool IsHeld => _phase == Phase.Held;

    public LayerKeyResult OnLayerKey(bool isDown, KeyModifiers modifiers, long nowMs)
    {
        if (isDown)
        {
            switch (_phase)
            {
                case Phase.Held:
                    return new LayerKeyResult(Swallow: true, Tapped: false); // auto-repeat
                case Phase.PassingThrough:
                    return new LayerKeyResult(Swallow: false, Tapped: false);
            }

            if ((modifiers & KeyModifiers.Shift) != 0)
            {
                _phase = Phase.PassingThrough;
                return new LayerKeyResult(Swallow: false, Tapped: false);
            }

            _phase = Phase.Held;
            _downAtMs = nowMs;
            _usedAsLayer = false;
            return new LayerKeyResult(Swallow: true, Tapped: false);
        }

        var phase = _phase;
        _phase = Phase.Idle;

        return phase switch
        {
            Phase.Held => new LayerKeyResult(
                Swallow: true,
                Tapped: !_usedAsLayer && nowMs - _downAtMs <= TapTimeoutMs),
            _ => new LayerKeyResult(Swallow: false, Tapped: false),
        };
    }

    /// <summary>Call for every other key-down, so typing while holding never counts as a tap.</summary>
    public void OnOtherKeyDown()
    {
        if (_phase == Phase.Held) _usedAsLayer = true;
    }
}
