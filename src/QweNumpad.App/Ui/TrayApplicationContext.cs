using QweNumpad.App.Detection;
using QweNumpad.App.Input;
using QweNumpad.Core;

namespace QweNumpad.App.Ui;

/// <summary>Wires everything together and owns the tray icon.</summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly string _settingsPath = AppSettings.DefaultPath;
    private readonly LearnedFieldStore _store = new(LearnedFieldStore.DefaultPath);
    private readonly ModeState _modeState = new();
    private readonly TrayIcons _icons = new();
    private readonly NotifyIcon _tray;
    private readonly Control _marshaller = new();
    private readonly UiaFocusWatcher _watcher;
    private readonly KeystrokeRouter _router;
    private readonly LowLevelKeyboardHook _hook;

    private AppSettings _settings;
    private FieldHeuristics _heuristics;
    private FieldClassifier _classifier;
    private FieldInfo? _currentField;
    private DiagnosticsForm? _diagnostics;
    private int _balloonsShown;

    public TrayApplicationContext(bool openDiagnostics = false)
    {
        _settings = AppSettings.Load(_settingsPath);
        _heuristics = new FieldHeuristics(_settings.ExtraKeywords);
        _classifier = new FieldClassifier(_heuristics, _store);

        // A hidden control gives us a reliable way back onto the UI thread.
        _marshaller.CreateControl();

        _router = new KeystrokeRouter(
            _modeState, new MappingEngine(_settings.BuildKeyMap()), _settings.OverrideHotkey);
        _router.OverrideHotkeyPressed += (_, _) => CycleOverride();

        // Installed from the UI thread, so hook callbacks arrive on the UI thread too.
        _hook = new LowLevelKeyboardHook(_router.Handle);

        _watcher = new UiaFocusWatcher();
        _watcher.FieldChanged += OnFieldChanged;

        _tray = new NotifyIcon
        {
            Icon = _icons.For(_modeState),
            Text = "QweNumpad",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip(),
        };
        _tray.ContextMenuStrip.Opening += (_, _) => BuildMenu();
        _tray.DoubleClick += (_, _) => ShowSettings();

        _modeState.Changed += (_, _) => RunOnUi(() =>
        {
            RefreshTray();
            RefreshDiagnostics();
        });

        RefreshTray();

        if (openDiagnostics) ShowDiagnostics();
    }

    private void RebuildClassifier()
    {
        _heuristics = new FieldHeuristics(_settings.ExtraKeywords);
        _classifier = new FieldClassifier(_heuristics, _store);
    }

    private void OnFieldChanged(object? sender, FieldInfo? field)
    {
        _currentField = field;

        var numeric = field is not null && _classifier.IsNumericField(field);
        _modeState.SetAutoDetected(numeric);

        RunOnUi(RefreshDiagnostics);
    }

    private void ShowDiagnostics()
    {
        if (_diagnostics is { IsDisposed: false })
        {
            _diagnostics.Activate();
            return;
        }

        _diagnostics = new DiagnosticsForm();
        _diagnostics.FormClosed += (_, _) => _diagnostics = null;
        _diagnostics.Show();
        RefreshDiagnostics();
    }

    private void RefreshDiagnostics() =>
        _diagnostics?.Update(_currentField, _modeState, _heuristics, _store);

    private void CycleOverride()
    {
        var mode = _modeState.CycleOverride();
        var field = _currentField;

        // The override doubles as teaching: correct it once here, and detection is
        // right the next time this same field gets focus.
        if (field is not null)
        {
            switch (mode)
            {
                case ModeOverride.ForcedOn:
                    _classifier.Teach(field, numeric: true);
                    break;
                case ModeOverride.ForcedOff:
                    _classifier.Teach(field, numeric: false);
                    break;
                default:
                    _store.Forget(field.Signature);
                    break;
            }
        }

        RefreshTray();
        ShowModeBalloon(mode);
    }

    private void ShowModeBalloon(ModeOverride mode)
    {
        if (!_settings.NotifyOnModeChange || _balloonsShown >= 5) return;

        _balloonsShown++;
        _tray.ShowBalloonTip(1200, "QweNumpad", mode switch
        {
            ModeOverride.ForcedOn => "Zorla açık — qwe/asd/zxc numpad",
            ModeOverride.ForcedOff => "Zorla kapalı — normal harfler",
            _ => "Otomatik — alana göre karar verilir",
        }, ToolTipIcon.None);
    }

    private void RefreshTray()
    {
        _tray.Icon = _icons.For(_modeState);

        var mode = _modeState.Override switch
        {
            ModeOverride.ForcedOn => "Zorla açık",
            ModeOverride.ForcedOff => "Zorla kapalı",
            _ => _modeState.IsActive ? "Otomatik (açık)" : "Otomatik (kapalı)",
        };

        // NotifyIcon.Text is capped at 63 characters by Windows.
        var text = $"QweNumpad — {mode}";
        _tray.Text = text.Length > 62 ? text[..62] : text;
    }

    private void BuildMenu()
    {
        var menu = _tray.ContextMenuStrip!;
        menu.Items.Clear();

        menu.Items.Add(new ToolStripLabel($"QweNumpad {AppInfo.Version}") { Font = new Font(menu.Font, FontStyle.Bold) });
        menu.Items.Add(new ToolStripLabel($"Durum: {(_modeState.IsActive ? "AÇIK" : "kapalı")}"));
        menu.Items.Add(new ToolStripSeparator());

        AddModeItem(menu, "Otomatik", ModeOverride.Auto);
        AddModeItem(menu, "Zorla açık", ModeOverride.ForcedOn);
        AddModeItem(menu, "Zorla kapalı", ModeOverride.ForcedOff);

        menu.Items.Add(new ToolStripSeparator());

        var field = _currentField;
        menu.Items.Add(new ToolStripLabel(field is null
            ? "Odakta alan yok"
            : Shorten($"Odak: {field.Signature}", 60)));

        var teachNumeric = new ToolStripMenuItem("Bu alanı sayısal öğren", null,
            (_, _) => TeachCurrent(true)) { Enabled = field is not null };
        var teachText = new ToolStripMenuItem("Bu alanı metin öğren", null,
            (_, _) => TeachCurrent(false)) { Enabled = field is not null };
        var forget = new ToolStripMenuItem("Bu alanın kuralını sil", null,
            (_, _) => ForgetCurrent()) { Enabled = field is not null };

        menu.Items.Add(teachNumeric);
        menu.Items.Add(teachText);
        menu.Items.Add(forget);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem(
            $"Ayarlar… ({_settings.OverrideHotkey})", null, (_, _) => ShowSettings()));
        menu.Items.Add(new ToolStripMenuItem("Tanılama…", null, (_, _) => ShowDiagnostics()));
        menu.Items.Add(new ToolStripMenuItem("GitHub sayfası", null, (_, _) => OpenUrl(AppInfo.GitHubUrl)));
        menu.Items.Add(new ToolStripMenuItem("Hata bildir…", null, (_, _) => OpenUrl(AppInfo.NewIssueUrl)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Çıkış", null, (_, _) => ExitApp()));
    }

    private void AddModeItem(ContextMenuStrip menu, string text, ModeOverride mode)
    {
        var item = new ToolStripMenuItem(text, null, (_, _) =>
        {
            _modeState.SetOverride(mode);
            RefreshTray();
        })
        {
            Checked = _modeState.Override == mode,
            CheckOnClick = false,
        };

        menu.Items.Add(item);
    }

    private void TeachCurrent(bool numeric)
    {
        if (_currentField is null) return;

        _classifier.Teach(_currentField, numeric);
        _modeState.SetAutoDetected(_classifier.IsNumericField(_currentField));
        RefreshTray();
    }

    private void ForgetCurrent()
    {
        if (_currentField is null) return;

        _store.Forget(_currentField.Signature);
        _modeState.SetAutoDetected(_classifier.IsNumericField(_currentField));
        RefreshTray();
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings, _store);
        if (form.ShowDialog() != DialogResult.OK) return;

        _settings = form.Result;
        _settings.Save(_settingsPath);

        StartupRegistry.Set(_settings.StartWithWindows);

        RebuildClassifier();
        _router.Reconfigure(new MappingEngine(_settings.BuildKeyMap()), _settings.OverrideHotkey);

        if (_currentField is not null)
            _modeState.SetAutoDetected(_classifier.IsNumericField(_currentField));

        RefreshTray();
        RefreshDiagnostics();
    }

    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            // No default browser; nothing useful to do.
        }
    }

    private static string Shorten(string text, int max) =>
        text.Length <= max ? text : text[..(max - 1)] + "…";

    private void RunOnUi(Action action)
    {
        if (_marshaller.IsDisposed) return;

        if (_marshaller.InvokeRequired) _marshaller.BeginInvoke(action);
        else action();
    }

    private void ExitApp()
    {
        _tray.Visible = false;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hook.Dispose();
            _watcher.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _icons.Dispose();
            _marshaller.Dispose();
        }

        base.Dispose(disposing);
    }
}
