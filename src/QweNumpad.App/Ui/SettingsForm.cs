using QweNumpad.Core;

namespace QweNumpad.App.Ui;

internal sealed class SettingsForm : Form
{
    private static readonly VirtualKey[] MapTargets =
    {
        VirtualKey.Numpad7, VirtualKey.Numpad8, VirtualKey.Numpad9,
        VirtualKey.Numpad4, VirtualKey.Numpad5, VirtualKey.Numpad6,
        VirtualKey.Numpad1, VirtualKey.Numpad2, VirtualKey.Numpad3,
        VirtualKey.Numpad0, VirtualKey.Decimal,
        VirtualKey.Add, VirtualKey.Subtract,
    };

    private readonly LearnedFieldStore _store;
    private readonly Dictionary<VirtualKey, int> _sourceByTarget = new();

    private readonly Button _hotkeyButton = new();
    private readonly TextBox _keywords = new();
    private readonly CheckBox _startup = new();
    private readonly CheckBox _notify = new();
    private readonly ListView _mapList = new();
    private readonly ListBox _learned = new();
    private readonly ComboBox _layerKey = new();
    private readonly AppSettings _original;

    private HotkeyDefinition _hotkey;

    public SettingsForm(AppSettings settings, LearnedFieldStore store)
    {
        _store = store;
        _original = settings;
        _hotkey = new HotkeyDefinition
        {
            KeyCode = settings.OverrideHotkey.KeyCode,
            Control = settings.OverrideHotkey.Control,
            Alt = settings.OverrideHotkey.Alt,
            Shift = settings.OverrideHotkey.Shift,
            Windows = settings.OverrideHotkey.Windows,
        };

        foreach (var (source, target) in settings.BuildKeyMap().Entries)
            _sourceByTarget[target] = source;

        Result = settings;

        BuildLayout(settings);
        ReloadMapList();
        ReloadLearnedList();
    }

    public AppSettings Result { get; private set; }

    private void BuildLayout(AppSettings settings)
    {
        Text = "QweNumpad — Ayarlar";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(560, 620);
        Padding = new Padding(12);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 10,
            AutoSize = false,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // --- Hotkey ---
        _hotkeyButton.Text = _hotkey.ToString();
        _hotkeyButton.Width = 200;
        _hotkeyButton.Click += (_, _) => CaptureHotkey();

        layout.Controls.Add(Section("Zorla aç/kapa kısayolu (Oto → Açık → Kapalı)"));
        layout.Controls.Add(_hotkeyButton);

        // --- Layer key ---
        _layerKey.DropDownStyle = ComboBoxStyle.DropDownList;
        _layerKey.Width = 200;
        _layerKey.Items.AddRange(LayerKeyChoices);
        _layerKey.SelectedItem = LayerKeyChoices.FirstOrDefault(c => c.KeyCode == settings.LayerKey)
                                 ?? LayerKeyChoices[0];

        layout.Controls.Add(Section("Katman tuşu (basılı tut: rakam · dokun: numpad kilidi · Shift+tuş: kendi işlevi)"));
        layout.Controls.Add(_layerKey);

        // --- Key map ---
        _mapList.View = View.Details;
        _mapList.FullRowSelect = true;
        _mapList.MultiSelect = false;
        _mapList.Height = 200;
        _mapList.Dock = DockStyle.Top;
        _mapList.Columns.Add("Numpad tuşu", 180);
        _mapList.Columns.Add("Basılacak tuş", 180);
        _mapList.DoubleClick += (_, _) => RemapSelected();

        var mapButtons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        var remap = new Button { Text = "Tuş ata", Width = 110 };
        remap.Click += (_, _) => RemapSelected();
        var clearKey = new Button { Text = "Atamayı kaldır", Width = 130 };
        clearKey.Click += (_, _) => ClearSelected();
        var resetMap = new Button { Text = "Varsayılana dön", Width = 130 };
        resetMap.Click += (_, _) => ResetMap();
        mapButtons.Controls.AddRange(new Control[] { remap, clearKey, resetMap });

        layout.Controls.Add(Section("Tuş haritası"));
        layout.Controls.Add(_mapList);
        layout.Controls.Add(mapButtons);

        // --- Keywords ---
        _keywords.Multiline = true;
        _keywords.Height = 60;
        _keywords.Dock = DockStyle.Top;
        _keywords.ScrollBars = ScrollBars.Vertical;
        _keywords.Text = string.Join(", ", settings.ExtraKeywords);

        layout.Controls.Add(Section("Ek sayısal alan kelimeleri (virgülle ayır)"));
        layout.Controls.Add(_keywords);

        // --- Learned fields ---
        _learned.Height = 110;
        _learned.Dock = DockStyle.Top;

        var learnedButtons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        var deleteOne = new Button { Text = "Seçileni sil", Width = 120 };
        deleteOne.Click += (_, _) => DeleteSelectedLearned();
        var deleteAll = new Button { Text = "Tümünü sil", Width = 120 };
        deleteAll.Click += (_, _) => DeleteAllLearned();
        learnedButtons.Controls.AddRange(new Control[] { deleteOne, deleteAll });

        layout.Controls.Add(Section("Öğrenilen alanlar"));
        layout.Controls.Add(_learned);
        layout.Controls.Add(learnedButtons);

        // --- Toggles ---
        _startup.Text = "Windows açılışında başlat";
        _startup.AutoSize = true;
        _startup.Checked = settings.StartWithWindows || StartupRegistry.IsEnabled();

        _notify.Text = "Mod değişiminde bildirim göster (ilk 5 kez)";
        _notify.AutoSize = true;
        _notify.Checked = settings.NotifyOnModeChange;

        layout.Controls.Add(_startup);
        layout.Controls.Add(_notify);

        // --- Buttons ---
        var ok = new Button { Text = "Kaydet", DialogResult = DialogResult.OK, Width = 100 };
        ok.Click += (_, _) => Collect();
        var cancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Width = 100 };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
        };
        actions.Controls.AddRange(new Control[] { ok, cancel });

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        scroll.Controls.Add(layout);
        layout.AutoSize = true;
        layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        Controls.Add(scroll);
        Controls.Add(actions);

        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static Label Section(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
        Padding = new Padding(0, 10, 0, 2),
    };

    private void CaptureHotkey()
    {
        using var dialog = new KeyCaptureDialog(
            "Kısayol için bir tuş kombinasyonu bas (Ctrl veya Alt şart).\nİptal için Esc.",
            captureModifiers: true);

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _hotkey = new HotkeyDefinition
        {
            KeyCode = dialog.KeyCode,
            Control = dialog.Control,
            Alt = dialog.Alt,
            Shift = dialog.Shift,
            Windows = false,
        };

        _hotkeyButton.Text = DescribeHotkey(_hotkey);
    }

    private static string DescribeHotkey(HotkeyDefinition hotkey)
    {
        var parts = new List<string>();
        if (hotkey.Control) parts.Add("Ctrl");
        if (hotkey.Alt) parts.Add("Alt");
        if (hotkey.Shift) parts.Add("Shift");
        parts.Add(((Keys)hotkey.KeyCode).ToString());
        return string.Join("+", parts);
    }

    private void ReloadMapList()
    {
        _mapList.Items.Clear();

        foreach (var target in MapTargets)
        {
            var source = _sourceByTarget.TryGetValue(target, out var vk)
                ? ((Keys)vk).ToString()
                : "—";

            var item = new ListViewItem(new[] { Describe(target), source }) { Tag = target };
            _mapList.Items.Add(item);
        }
    }

    private static string Describe(VirtualKey key) => key switch
    {
        >= VirtualKey.Numpad0 and <= VirtualKey.Numpad9 =>
            $"Numpad {(int)key - (int)VirtualKey.Numpad0}",
        VirtualKey.Decimal => "Numpad , (ondalık)",
        VirtualKey.Add => "Numpad +",
        VirtualKey.Subtract => "Numpad -",
        _ => key.ToString(),
    };

    private void RemapSelected()
    {
        if (_mapList.SelectedItems.Count == 0) return;
        var target = (VirtualKey)_mapList.SelectedItems[0].Tag!;

        using var dialog = new KeyCaptureDialog(
            $"{Describe(target)} için basılacak tuşa bas.\nİptal için Esc.",
            captureModifiers: false);

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        // One physical key cannot drive two numpad keys.
        foreach (var existing in _sourceByTarget.Where(p => p.Value == dialog.KeyCode).ToList())
            _sourceByTarget.Remove(existing.Key);

        _sourceByTarget[target] = dialog.KeyCode;
        ReloadMapList();
    }

    private void ClearSelected()
    {
        if (_mapList.SelectedItems.Count == 0) return;

        _sourceByTarget.Remove((VirtualKey)_mapList.SelectedItems[0].Tag!);
        ReloadMapList();
    }

    private void ResetMap()
    {
        _sourceByTarget.Clear();
        foreach (var (source, target) in KeyMap.Default.Entries)
            _sourceByTarget[target] = source;

        ReloadMapList();
    }

    private void ReloadLearnedList()
    {
        _learned.Items.Clear();

        foreach (var entry in _store.Entries.OrderBy(e => e.Description))
            _learned.Items.Add(new LearnedListItem(entry));
    }

    private void DeleteSelectedLearned()
    {
        if (_learned.SelectedItem is not LearnedListItem item) return;

        _store.ForgetKey(item.Entry.Key);
        ReloadLearnedList();
    }

    private void DeleteAllLearned()
    {
        var answer = MessageBox.Show(
            this,
            "Öğrenilen tüm alan kuralları silinecek. Bu geri alınamaz. Devam edilsin mi?",
            "QweNumpad",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes) return;

        _store.Clear();
        ReloadLearnedList();
    }

    private void Collect()
    {
        var map = new Dictionary<int, VirtualKey>();
        foreach (var (target, source) in _sourceByTarget)
            map[source] = target;

        Result = new AppSettings
        {
            OverrideHotkey = _hotkey,
            ExtraKeywords = _keywords.Text
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .Where(k => k.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CustomMap = IsDefaultMap(map) ? new Dictionary<int, VirtualKey>() : map,
            StartWithWindows = _startup.Checked,
            NotifyOnModeChange = _notify.Checked,
            LayerKey = ((LayerKeyChoice)_layerKey.SelectedItem!).KeyCode,
            // Not editable here, but must survive a save.
            WelcomeShown = _original.WelcomeShown,
        };
    }

    private sealed record LayerKeyChoice(int KeyCode, string Label)
    {
        public override string ToString() => Label;
    }

    private static readonly LayerKeyChoice[] LayerKeyChoices =
    {
        new(LayerKeys.CapsLock, "CapsLock"),
        new(LayerKeys.RightControl, "Sağ Ctrl"),
        new(LayerKeys.None, "Kapalı"),
    };

    private static bool IsDefaultMap(Dictionary<int, VirtualKey> map)
    {
        var def = KeyMap.Default.Entries;
        return map.Count == def.Count
               && map.All(p => def.TryGetValue(p.Key, out var v) && v == p.Value);
    }

    private sealed record LearnedListItem(LearnedField Entry)
    {
        public override string ToString() =>
            $"{(Entry.Numeric ? "SAYISAL" : "metin  ")}  {Entry.Description}";
    }
}
