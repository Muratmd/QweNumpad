namespace QweNumpad.Core;

public enum ModeOverride
{
    /// <summary>Follow field detection.</summary>
    Auto,

    /// <summary>Always remap, regardless of detection.</summary>
    ForcedOn,

    /// <summary>Never remap, regardless of detection.</summary>
    ForcedOff,
}

/// <summary>
/// Holds whether remapping is currently active. Written by the focus watcher and the
/// override hotkey, read by the keyboard hook via <see cref="IsActive"/>.
/// </summary>
public sealed class ModeState
{
    private volatile bool _autoDetected;
    private volatile ModeOverride _override = ModeOverride.Auto;
    private bool _lastEffective;
    private ModeOverride _lastOverride = ModeOverride.Auto;

    public event EventHandler? Changed;

    public ModeOverride Override => _override;

    public bool AutoDetected => _autoDetected;

    public bool IsActive => _override switch
    {
        ModeOverride.ForcedOn => true,
        ModeOverride.ForcedOff => false,
        _ => _autoDetected,
    };

    public void SetAutoDetected(bool detected)
    {
        _autoDetected = detected;
        RaiseIfEffectiveStateChanged();
    }

    public ModeOverride CycleOverride()
    {
        _override = _override switch
        {
            ModeOverride.Auto => ModeOverride.ForcedOn,
            ModeOverride.ForcedOn => ModeOverride.ForcedOff,
            _ => ModeOverride.Auto,
        };

        RaiseIfEffectiveStateChanged();
        return _override;
    }

    public void SetOverride(ModeOverride value)
    {
        _override = value;
        RaiseIfEffectiveStateChanged();
    }

    /// <summary>
    /// Fires when anything the tray icon shows changes: either remapping switched on/off,
    /// or the override moved between Auto / ForcedOn / ForcedOff.
    /// </summary>
    private void RaiseIfEffectiveStateChanged()
    {
        var effective = IsActive;
        var over = _override;
        if (effective == _lastEffective && over == _lastOverride) return;

        _lastEffective = effective;
        _lastOverride = over;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
