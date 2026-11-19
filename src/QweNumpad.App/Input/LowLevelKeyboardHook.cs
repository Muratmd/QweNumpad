using System.Runtime.InteropServices;
using QweNumpad.App.Interop;
using QweNumpad.Core;

namespace QweNumpad.App.Input;

/// <summary>
/// A WH_KEYBOARD_LL hook. The callback runs on the thread that installed it and blocks
/// the whole system's keyboard input until it returns, so it does the absolute minimum:
/// read the key, ask the handler, return. No UI Automation, no I/O, no locks.
/// </summary>
internal sealed class LowLevelKeyboardHook : IDisposable
{
    /// <summary>Return true to swallow the key, false to let it through.</summary>
    public delegate bool KeyHandler(int virtualKeyCode, bool isKeyDown, KeyModifiers modifiers);

    private readonly KeyHandler _handler;

    // Held in a field so the GC cannot collect the delegate while Windows still calls it.
    private readonly NativeMethods.LowLevelKeyboardProc _proc;

    private IntPtr _hook;

    public LowLevelKeyboardHook(KeyHandler handler)
    {
        _handler = handler;
        _proc = HookProc;

        var module = NativeMethods.GetModuleHandle(null);
        _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _proc, module, 0);

        if (_hook == IntPtr.Zero)
            throw new InvalidOperationException(
                $"Klavye kancası kurulamadı (Win32 hata {Marshal.GetLastWin32Error()}).");
    }

    private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

            // Ignore the keystrokes we injected ourselves, or we would recurse forever.
            if (data.dwExtraInfo != NativeMethods.InjectionTag)
            {
                var message = (int)wParam;
                var isDown = message is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN;
                var isUp = message is NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP;

                if (isDown || isUp)
                {
                    try
                    {
                        if (_handler((int)data.vkCode, isDown, ReadModifiers()))
                            return new IntPtr(1);
                    }
                    catch
                    {
                        // Never let an exception escape into the OS input path:
                        // Windows would silently uninstall the hook.
                    }
                }
            }
        }

        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private static KeyModifiers ReadModifiers()
    {
        var mods = KeyModifiers.None;

        if (IsDown(NativeMethods.VK_SHIFT)) mods |= KeyModifiers.Shift;
        if (IsDown(NativeMethods.VK_CONTROL)) mods |= KeyModifiers.Control;
        if (IsDown(NativeMethods.VK_MENU)) mods |= KeyModifiers.Alt;
        if (IsDown(NativeMethods.VK_LWIN) || IsDown(NativeMethods.VK_RWIN)) mods |= KeyModifiers.Windows;

        return mods;

        static bool IsDown(int vk) => (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;
    }

    public void Dispose()
    {
        if (_hook == IntPtr.Zero) return;

        NativeMethods.UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }
}
