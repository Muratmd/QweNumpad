using System.Diagnostics;
using System.Windows.Automation;
using QweNumpad.App.Interop;
using QweNumpad.Core;

namespace QweNumpad.App.Detection;

/// <summary>
/// Watches which field has keyboard focus and describes it as a <see cref="FieldInfo"/>.
/// </summary>
/// <remarks>
/// Runs entirely off the keyboard hook's thread. UI Automation property reads are COM
/// calls that can block for tens of milliseconds or throw when the element disappears
/// mid-read, which is exactly why detection never happens inside the hook.
/// A slow poll backs up the focus event, because some apps (notably Electron and custom
/// toolkits) do not raise focus-changed reliably.
/// </remarks>
internal sealed class UiaFocusWatcher : IDisposable
{
    private readonly AutomationFocusChangedEventHandler _handler;
    private readonly System.Threading.Timer _poll;
    private readonly Dictionary<int, string> _processNames = new();
    private readonly object _gate = new();

    private string? _lastFingerprint;
    private bool _disposed;

    public UiaFocusWatcher()
    {
        _handler = OnFocusChanged;
        Automation.AddAutomationFocusChangedEventHandler(_handler);
        _poll = new System.Threading.Timer(_ => PollFocusedElement(), null, 1500, 1500);
    }

    /// <summary>Fires with the newly focused field, or null when focus is not a usable field.</summary>
    public event EventHandler<FieldInfo?>? FieldChanged;

    public FieldInfo? Current { get; private set; }

    private void OnFocusChanged(object? sender, AutomationFocusChangedEventArgs e) =>
        Inspect(sender as AutomationElement);

    private void PollFocusedElement()
    {
        try
        {
            Inspect(AutomationElement.FocusedElement);
        }
        catch
        {
            // Focused element can vanish between the call and the read.
        }
    }

    private void Inspect(AutomationElement? element)
    {
        if (_disposed) return;

        FieldInfo? info;
        try
        {
            info = element is null ? null : Describe(element);
        }
        catch
        {
            info = null;
        }

        var fingerprint = Fingerprint(info);

        lock (_gate)
        {
            if (fingerprint == _lastFingerprint) return;
            _lastFingerprint = fingerprint;
            Current = info;
        }

        FieldChanged?.Invoke(this, info);
    }

    private FieldInfo Describe(AutomationElement element)
    {
        var current = element.Current;

        var controlType = ShortControlTypeName(current.ControlType);
        var name = current.Name ?? "";
        var automationId = current.AutomationId ?? "";
        var helpText = current.HelpText;

        if (name.Length == 0)
            name = TryReadLabel(element) ?? "";

        var signature = FieldSignature.Create(
            ProcessName(current.ProcessId), controlType, automationId, name);

        var hasRangeValue = element.TryGetCurrentPattern(RangeValuePattern.Pattern, out _);
        var isSpinner = current.ControlType == ControlType.Spinner;
        var isPassword = current.IsPassword;
        var isReadOnly = TryReadIsReadOnly(element);

        var (esNumber, esPassword, esReadOnly) = ReadWin32Styles(current.NativeWindowHandle);

        return new FieldInfo(
            signature,
            hasRangeValue,
            isSpinner,
            esNumber,
            helpText,
            Placeholder: null,
            IsPasswordField: isPassword || esPassword,
            IsReadOnly: isReadOnly || esReadOnly);
    }

    /// <summary>"ControlType.Edit" -> "Edit", which is what the Core heuristics expect.</summary>
    private static string ShortControlTypeName(ControlType? type)
    {
        var programmatic = type?.ProgrammaticName ?? "";
        var dot = programmatic.LastIndexOf('.');
        return dot >= 0 && dot < programmatic.Length - 1 ? programmatic[(dot + 1)..] : programmatic;
    }

    private static string? TryReadLabel(AutomationElement element)
    {
        try
        {
            return element.Current.LabeledBy?.Current.Name;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryReadIsReadOnly(AutomationElement element)
    {
        try
        {
            return element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern)
                   && pattern is ValuePattern value
                   && value.Current.IsReadOnly;
        }
        catch
        {
            return false;
        }
    }

    private static (bool Numeric, bool Password, bool ReadOnly) ReadWin32Styles(int nativeHandle)
    {
        if (nativeHandle == 0) return (false, false, false);

        try
        {
            var style = NativeMethods.GetWindowLong(new IntPtr(nativeHandle), NativeMethods.GWL_STYLE);

            return ((style & NativeMethods.ES_NUMBER) != 0,
                    (style & NativeMethods.ES_PASSWORD) != 0,
                    (style & NativeMethods.ES_READONLY) != 0);
        }
        catch
        {
            return (false, false, false);
        }
    }

    private string ProcessName(int processId)
    {
        lock (_gate)
        {
            if (_processNames.TryGetValue(processId, out var cached)) return cached;
        }

        string name;
        try
        {
            using var process = Process.GetProcessById(processId);
            name = process.ProcessName;
        }
        catch
        {
            name = processId.ToString();
        }

        lock (_gate)
        {
            // Bound the cache: process ids get recycled and this runs for the whole session.
            if (_processNames.Count > 256) _processNames.Clear();
            _processNames[processId] = name;
        }

        return name;
    }

    private static string Fingerprint(FieldInfo? info) =>
        info is null
            ? "<none>"
            : string.Join('#',
                info.Signature.Key,
                info.HasRangeValuePattern,
                info.IsSpinnerControlType,
                info.HasEsNumberStyle,
                info.IsPasswordField,
                info.IsReadOnly,
                info.HelpText ?? "");

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _poll.Dispose();

        try
        {
            Automation.RemoveAutomationFocusChangedEventHandler(_handler);
        }
        catch
        {
            // UI Automation can already be torn down during shutdown.
        }
    }
}
