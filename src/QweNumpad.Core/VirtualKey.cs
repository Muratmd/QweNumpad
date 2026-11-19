namespace QweNumpad.Core;

/// <summary>Numpad virtual-key codes we can emit. Values match Win32 VK_* constants.</summary>
public enum VirtualKey
{
    Numpad0 = 0x60,
    Numpad1 = 0x61,
    Numpad2 = 0x62,
    Numpad3 = 0x63,
    Numpad4 = 0x64,
    Numpad5 = 0x65,
    Numpad6 = 0x66,
    Numpad7 = 0x67,
    Numpad8 = 0x68,
    Numpad9 = 0x69,
    Multiply = 0x6A,
    Add = 0x6B,
    Separator = 0x6C,
    Subtract = 0x6D,
    Decimal = 0x6E,
    Divide = 0x6F,
}

/// <summary>Source virtual-key codes referenced by the default map and by tests.</summary>
public static class Vk
{
    public const int Space = 0x20;
    public const int A = 0x41;
    public const int C = 0x43;
    public const int D = 0x44;
    public const int E = 0x45;
    public const int F = 0x46;
    public const int K = 0x4B;
    public const int Q = 0x51;
    public const int R = 0x52;
    public const int S = 0x53;
    public const int V = 0x56;
    public const int W = 0x57;
    public const int X = 0x58;
    public const int Z = 0x5A;
}

[Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    Windows = 8,
}

public enum KeyAction
{
    /// <summary>Let the original key reach the foreground app untouched.</summary>
    Passthrough,

    /// <summary>Swallow the original key and emit <see cref="KeyDecision.Output"/> instead.</summary>
    Translate,
}

public readonly record struct KeyDecision(KeyAction Action, VirtualKey Output)
{
    public static readonly KeyDecision Passthrough = new(KeyAction.Passthrough, default);

    public static KeyDecision Translate(VirtualKey output) => new(KeyAction.Translate, output);
}
