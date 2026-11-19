namespace QweNumpad.App.Ui;

/// <summary>
/// First-run window, shown once next to the notification area.
/// </summary>
/// <remarks>
/// A tray-only app has no window, and Windows 11 parks new tray icons in the hidden "^"
/// flyout while Focus Assist swallows balloon tips. Without this, a first-time user sees
/// nothing at all after double-clicking the exe and cannot tell how to close it.
/// </remarks>
internal sealed class WelcomeForm : Form
{
    public WelcomeForm(string hotkey)
    {
        Text = "QweNumpad çalışıyor";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        var intro = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            Text = "QweNumpad arka planda çalışıyor. Sayı bekleyen bir alana tıkladığında " +
                   "sol elin altındaki tuşlar numpad olur:",
        };

        var map = new Label
        {
            AutoSize = true,
            Font = new Font(FontFamily.GenericMonospace, 10, FontStyle.Bold),
            Padding = new Padding(24, 8, 0, 8),
            Text = "Q W E  →  7 8 9\n" +
                   "A S D  →  4 5 6\n" +
                   "Z X C  →  1 2 3\n" +
                   "Space  →  0     V → ,",
        };

        var body = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            Text =
                "Her yerde rakam için CapsLock'u basılı tut. Tek dokunuş numpad kilidini " +
                "açar/kapatır; gerçek Caps Lock için Shift+CapsLock.\n\n" +
                $"Otomatik algılama yanlış karar verirse {hotkey} ile düzelt; o alanı öğrenir.\n\n" +
                "Simgesi sağ alttaki bildirim alanında (saat yanında). Görünmüyorsa ^ okuna " +
                "tıkla. Menü, Ayarlar ve Çıkış o simgeye tıklayınca açılır.",
        };

        var showInTaskbar = new Button
        {
            Text = "Simgeyi görev çubuğunda göster…",
            AutoSize = true,
        };
        showInTaskbar.Click += (_, _) => OpenTaskbarSettings();

        var ok = new Button { Text = "Tamam", AutoSize = true, DialogResult = DialogResult.OK };
        ok.Click += (_, _) => Close();
        AcceptButton = ok;

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 12, 0, 0),
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(showInTaskbar);

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 4 };
        layout.Controls.Add(intro);
        layout.Controls.Add(map);
        layout.Controls.Add(body);
        layout.Controls.Add(buttons);
        Controls.Add(layout);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Sit just above the notification area, where the icon is.
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        Location = new Point(area.Right - Width - 12, area.Bottom - Height - 12);
    }

    /// <summary>
    /// Windows offers no API to pin a tray icon; the per-app switch lives under
    /// Settings → Personalization → Taskbar → Other system tray icons.
    /// </summary>
    private static void OpenTaskbarSettings()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:taskbar")
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            // Settings app unavailable (stripped-down Windows); the text already explains the ^ flyout.
        }
    }
}
