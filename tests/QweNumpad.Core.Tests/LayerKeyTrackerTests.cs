using QweNumpad.Core;

namespace QweNumpad.Core.Tests;

public class LayerKeyTrackerTests
{
    [Fact]
    public void Press_holds_and_release_lets_go()
    {
        var tracker = new LayerKeyTracker();

        var down = tracker.OnLayerKey(isDown: true, KeyModifiers.None, nowMs: 0);
        Assert.True(down.Swallow);
        Assert.True(tracker.IsHeld);

        var up = tracker.OnLayerKey(isDown: false, KeyModifiers.None, nowMs: 1000);
        Assert.True(up.Swallow);
        Assert.False(tracker.IsHeld);
    }

    [Fact]
    public void Quick_press_without_other_keys_is_a_tap()
    {
        var tracker = new LayerKeyTracker();

        tracker.OnLayerKey(true, KeyModifiers.None, 0);
        var up = tracker.OnLayerKey(false, KeyModifiers.None, 120);

        Assert.True(up.Tapped);
    }

    [Fact]
    public void Slow_press_is_not_a_tap()
    {
        var tracker = new LayerKeyTracker();

        tracker.OnLayerKey(true, KeyModifiers.None, 0);
        var up = tracker.OnLayerKey(false, KeyModifiers.None, LayerKeyTracker.TapTimeoutMs + 1);

        Assert.False(up.Tapped);
    }

    [Fact]
    public void Typing_while_held_is_not_a_tap_even_if_quick()
    {
        var tracker = new LayerKeyTracker();

        tracker.OnLayerKey(true, KeyModifiers.None, 0);
        tracker.OnOtherKeyDown();
        var up = tracker.OnLayerKey(false, KeyModifiers.None, 100);

        Assert.False(up.Tapped);
    }

    [Fact]
    public void Auto_repeat_downs_are_swallowed_and_do_not_restart_the_timer()
    {
        var tracker = new LayerKeyTracker();

        tracker.OnLayerKey(true, KeyModifiers.None, 0);
        var repeat = tracker.OnLayerKey(true, KeyModifiers.None, 500);
        Assert.True(repeat.Swallow);
        Assert.False(repeat.Tapped);

        var up = tracker.OnLayerKey(false, KeyModifiers.None, 550);
        Assert.False(up.Tapped);
    }

    [Fact]
    public void Shift_passes_the_key_through_for_a_real_caps_lock()
    {
        var tracker = new LayerKeyTracker();

        var down = tracker.OnLayerKey(true, KeyModifiers.Shift, 0);
        var up = tracker.OnLayerKey(false, KeyModifiers.Shift, 50);

        Assert.False(down.Swallow);
        Assert.False(up.Swallow);
        Assert.False(up.Tapped);
        Assert.False(tracker.IsHeld);
    }

    [Fact]
    public void Release_after_shift_passthrough_stays_passthrough_even_without_shift()
    {
        // Shift released before CapsLock: the key-up must still reach Windows,
        // or Windows sees a CapsLock down with no up.
        var tracker = new LayerKeyTracker();

        tracker.OnLayerKey(true, KeyModifiers.Shift, 0);
        var up = tracker.OnLayerKey(false, KeyModifiers.None, 50);

        Assert.False(up.Swallow);
    }

    [Fact]
    public void Stray_release_without_press_is_ignored()
    {
        var tracker = new LayerKeyTracker();

        var up = tracker.OnLayerKey(false, KeyModifiers.None, 10);

        Assert.False(up.Swallow);
        Assert.False(up.Tapped);
    }

    [Fact]
    public void Other_keys_when_not_held_do_nothing()
    {
        var tracker = new LayerKeyTracker();
        tracker.OnOtherKeyDown();

        tracker.OnLayerKey(true, KeyModifiers.None, 0);
        var up = tracker.OnLayerKey(false, KeyModifiers.None, 50);

        Assert.True(up.Tapped);
    }
}
