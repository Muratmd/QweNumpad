using QweNumpad.App.Interop;
using QweNumpad.Core;

namespace QweNumpad.App.Input;

/// <summary>
/// Synthesizes the replacement keystroke.
/// </summary>
/// <remarks>
/// Numpad virtual keys only produce digits while Num Lock is on — with Num Lock off,
/// VK_NUMPAD7 means Home. So when Num Lock is off we send the top-row digit key
/// instead. That keeps the output correct without silently toggling the user's Num Lock.
/// </remarks>
internal static class KeyOutputSender
{
    public static void Send(VirtualKey key, bool keyUp)
    {
        var vk = (ushort)Resolve(key);
        var scan = (ushort)NativeMethods.MapVirtualKey(vk, NativeMethods.MAPVK_VK_TO_VSC);

        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = vk,
                    wScan = scan,
                    dwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = NativeMethods.InjectionTag,
                },
            },
        };

        NativeMethods.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static int Resolve(VirtualKey key)
    {
        var numLockOn = (NativeMethods.GetKeyState(NativeMethods.VK_NUMLOCK) & 1) != 0;
        if (numLockOn) return (int)key;

        return key switch
        {
            >= VirtualKey.Numpad0 and <= VirtualKey.Numpad9 =>
                0x30 + (key - VirtualKey.Numpad0),
            VirtualKey.Decimal => 0xBE,  // VK_OEM_PERIOD
            VirtualKey.Add => 0xBB,      // VK_OEM_PLUS
            VirtualKey.Subtract => 0xBD, // VK_OEM_MINUS
            _ => (int)key,
        };
    }
}
