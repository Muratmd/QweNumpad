using Microsoft.Win32;

namespace QweNumpad.App;

/// <summary>
/// Moves our tray icon out of the Windows 11 overflow flyout ("^") onto the taskbar, once.
/// </summary>
/// <remarks>
/// Explorer records every notification icon under HKCU\Control Panel\NotifyIconSettings, one
/// subkey per executable. New icons get no IsPromoted value and land in the overflow, where a
/// tray-only app is effectively invisible. We set IsPromoted=1 only while the value is absent:
/// once the user hides the icon themselves, Explorer writes 0 and we never touch it again.
/// The subkey appears a moment after the icon is first shown, hence the short polling loop.
/// </remarks>
internal static class TrayPromoter
{
    private const string RootKey = @"Control Panel\NotifyIconSettings";

    /// <summary>Raised on a thread-pool thread when the icon was promoted for the first time.</summary>
    public static void PromoteInBackground(Action onFirstPromotion)
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return;

        _ = Task.Run(async () =>
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                await Task.Delay(500).ConfigureAwait(false);

                var result = TryPromote(exePath);
                if (result == Result.Promoted) onFirstPromotion();
                if (result != Result.NotRegisteredYet) return;
            }
        });
    }

    private enum Result { NotRegisteredYet, AlreadyDecided, Promoted }

    private static Result TryPromote(string exePath)
    {
        try
        {
            using var root = Registry.CurrentUser.OpenSubKey(RootKey);
            if (root is null) return Result.NotRegisteredYet;

            foreach (var name in root.GetSubKeyNames())
            {
                using var entry = root.OpenSubKey(name, writable: true);
                if (entry?.GetValue("ExecutablePath") is not string path) continue;
                if (!string.Equals(path, exePath, StringComparison.OrdinalIgnoreCase)) continue;

                if (entry.GetValue("IsPromoted") is not null) return Result.AlreadyDecided;

                entry.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
                return Result.Promoted;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or System.IO.IOException)
        {
            return Result.AlreadyDecided;
        }

        return Result.NotRegisteredYet;
    }
}
