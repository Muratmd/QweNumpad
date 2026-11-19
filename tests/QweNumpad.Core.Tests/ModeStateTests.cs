using QweNumpad.Core;

namespace QweNumpad.Core.Tests;

public class ModeStateTests
{
    [Fact]
    public void Starts_in_auto_and_inactive()
    {
        var state = new ModeState();

        Assert.Equal(ModeOverride.Auto, state.Override);
        Assert.False(state.IsActive);
    }

    [Fact]
    public void Auto_follows_detection()
    {
        var state = new ModeState();

        state.SetAutoDetected(true);
        Assert.True(state.IsActive);

        state.SetAutoDetected(false);
        Assert.False(state.IsActive);
    }

    [Fact]
    public void Forced_on_wins_over_negative_detection()
    {
        var state = new ModeState();
        state.SetAutoDetected(false);

        state.CycleOverride();

        Assert.Equal(ModeOverride.ForcedOn, state.Override);
        Assert.True(state.IsActive);
    }

    [Fact]
    public void Forced_off_wins_over_positive_detection()
    {
        var state = new ModeState();
        state.SetAutoDetected(true);

        state.CycleOverride();
        state.CycleOverride();

        Assert.Equal(ModeOverride.ForcedOff, state.Override);
        Assert.False(state.IsActive);
    }

    [Fact]
    public void Cycle_returns_to_auto()
    {
        var state = new ModeState();

        Assert.Equal(ModeOverride.ForcedOn, state.CycleOverride());
        Assert.Equal(ModeOverride.ForcedOff, state.CycleOverride());
        Assert.Equal(ModeOverride.Auto, state.CycleOverride());
    }

    [Fact]
    public void Raises_changed_only_when_effective_state_changes()
    {
        var state = new ModeState();
        var count = 0;
        state.Changed += (_, _) => count++;

        state.SetAutoDetected(true);
        Assert.Equal(1, count);

        state.SetAutoDetected(true);
        Assert.Equal(1, count);

        state.CycleOverride();
        Assert.Equal(2, count);
    }
}
