namespace QweNumpad.Core;

/// <summary>
/// Which physical key becomes which numpad key. The default map mirrors the numpad's
/// 3x3 digit grid onto the left hand's home position:
/// <code>
/// q w e -> 7 8 9
/// a s d -> 4 5 6
/// z x c -> 1 2 3
/// space -> 0        v -> decimal separator
/// </code>
/// </summary>
public sealed class KeyMap
{
    private readonly Dictionary<int, VirtualKey> _map;

    private KeyMap(Dictionary<int, VirtualKey> map) => _map = map;

    public static KeyMap Default { get; } = FromPairs(new Dictionary<int, VirtualKey>
    {
        [Vk.Q] = VirtualKey.Numpad7,
        [Vk.W] = VirtualKey.Numpad8,
        [Vk.E] = VirtualKey.Numpad9,
        [Vk.A] = VirtualKey.Numpad4,
        [Vk.S] = VirtualKey.Numpad5,
        [Vk.D] = VirtualKey.Numpad6,
        [Vk.Z] = VirtualKey.Numpad1,
        [Vk.X] = VirtualKey.Numpad2,
        [Vk.C] = VirtualKey.Numpad3,
        [Vk.Space] = VirtualKey.Numpad0,
        [Vk.V] = VirtualKey.Decimal,
    });

    public static KeyMap FromPairs(IEnumerable<KeyValuePair<int, VirtualKey>> pairs) =>
        new(new Dictionary<int, VirtualKey>(pairs));

    public IReadOnlyDictionary<int, VirtualKey> Entries => _map;

    public bool TryGet(int virtualKeyCode, out VirtualKey output) =>
        _map.TryGetValue(virtualKeyCode, out output);
}
