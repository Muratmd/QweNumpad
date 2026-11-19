using Microsoft.Win32;

namespace QweNumpad.App;

/// <summary>Per-user "run at login" entry. No admin rights, no scheduled task, no service.</summary>
internal static class StartupRegistry
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "QweNumpad";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string value && value.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null) return;

            if (!enabled)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
                return;
            }

            var path = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(path)) key.SetValue(ValueName, $"\"{path}\"");
        }
        catch
        {
            // A locked-down registry just means the toggle does nothing.
        }
    }
}
