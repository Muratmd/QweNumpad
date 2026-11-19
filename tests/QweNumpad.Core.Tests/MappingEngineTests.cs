using QweNumpad.Core;

namespace QweNumpad.Core.Tests;

public class MappingEngineTests
{
    private readonly MappingEngine _engine = new(KeyMap.Default);

    [Theory]
    [InlineData(Vk.Q, VirtualKey.Numpad7)]
    [InlineData(Vk.W, VirtualKey.Numpad8)]
    [InlineData(Vk.E, VirtualKey.Numpad9)]
    [InlineData(Vk.A, VirtualKey.Numpad4)]
    [InlineData(Vk.S, VirtualKey.Numpad5)]
    [InlineData(Vk.D, VirtualKey.Numpad6)]
    [InlineData(Vk.Z, VirtualKey.Numpad1)]
    [InlineData(Vk.X, VirtualKey.Numpad2)]
    [InlineData(Vk.C, VirtualKey.Numpad3)]
    [InlineData(Vk.Space, VirtualKey.Numpad0)]
    [InlineData(Vk.V, VirtualKey.Decimal)]
    public void Translates_mapped_keys_when_mode_active(int vk, VirtualKey expected)
    {
        var decision = _engine.Decide(vk, KeyModifiers.None, modeActive: true);

        Assert.Equal(KeyAction.Translate, decision.Action);
        Assert.Equal(expected, decision.Output);
    }

    [Fact]
    public void Passes_through_everything_when_mode_inactive()
    {
        var decision = _engine.Decide(Vk.Q, KeyModifiers.None, modeActive: false);

        Assert.Equal(KeyAction.Passthrough, decision.Action);
    }

    [Theory]
    [InlineData(KeyModifiers.Control)]
    [InlineData(KeyModifiers.Alt)]
    [InlineData(KeyModifiers.Windows)]
    [InlineData(KeyModifiers.Shift)]
    [InlineData(KeyModifiers.Control | KeyModifiers.Shift)]
    public void Passes_through_when_any_modifier_held(KeyModifiers mods)
    {
        var decision = _engine.Decide(Vk.Q, mods, modeActive: true);

        Assert.Equal(KeyAction.Passthrough, decision.Action);
    }

    [Fact]
    public void Passes_through_unmapped_keys()
    {
        var decision = _engine.Decide(Vk.K, KeyModifiers.None, modeActive: true);

        Assert.Equal(KeyAction.Passthrough, decision.Action);
    }

    [Fact]
    public void Default_map_does_not_claim_digit_row_or_navigation_keys()
    {
        foreach (var vk in new[] { 0x31, 0x08, 0x0D, 0x09, 0x1B, 0x25, 0x27 })
        {
            Assert.Equal(KeyAction.Passthrough, _engine.Decide(vk, KeyModifiers.None, true).Action);
        }
    }

    [Fact]
    public void Custom_map_replaces_defaults()
    {
        var map = KeyMap.FromPairs(new Dictionary<int, VirtualKey> { [Vk.K] = VirtualKey.Numpad9 });
        var engine = new MappingEngine(map);

        Assert.Equal(KeyAction.Translate, engine.Decide(Vk.K, KeyModifiers.None, true).Action);
        Assert.Equal(KeyAction.Passthrough, engine.Decide(Vk.Q, KeyModifiers.None, true).Action);
    }
}
