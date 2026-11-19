namespace QweNumpad.App.Ui;

/// <summary>Modal "press a key" prompt used for the hotkey and for remapping a source key.</summary>
internal sealed class KeyCaptureDialog : Form
{
    private readonly Label _label;

    public KeyCaptureDialog(string prompt, bool captureModifiers)
    {
        CaptureModifiers = captureModifiers;

        Text = "Tuş yakala";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(360, 110);
        KeyPreview = true;

        _label = new Label
        {
            Text = prompt,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(12),
        };

        Controls.Add(_label);

        var cancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Visible = false };
        Controls.Add(cancel);
        CancelButton = cancel;
    }

    public bool CaptureModifiers { get; }

    public int KeyCode { get; private set; }

    public bool Control { get; private set; }

    public bool Alt { get; private set; }

    public bool Shift { get; private set; }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var key = e.KeyCode;

        if (key is Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        // Ignore the modifiers themselves: we want the key they modify.
        if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu
            or Keys.LControlKey or Keys.RControlKey
            or Keys.LShiftKey or Keys.RShiftKey
            or Keys.LMenu or Keys.RMenu
            or Keys.LWin or Keys.RWin)
        {
            e.Handled = true;
            return;
        }

        if (CaptureModifiers && !(e.Control || e.Alt))
        {
            _label.Text = "Kısayol en az Ctrl veya Alt içermeli. Tekrar dene.";
            e.Handled = true;
            return;
        }

        KeyCode = (int)key;
        Control = e.Control;
        Alt = e.Alt;
        Shift = e.Shift;

        e.Handled = true;
        DialogResult = DialogResult.OK;
        Close();
    }
}
