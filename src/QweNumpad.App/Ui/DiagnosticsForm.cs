using QweNumpad.Core;

namespace QweNumpad.App.Ui;

/// <summary>
/// Shows what the detector sees for the currently focused field, plus a test box to type into.
/// </summary>
/// <remarks>
/// This is the tool users should open when detection misfires: it names the exact signature,
/// which flags fired, and the final verdict, which is what a useful bug report needs.
/// The automated smoke test drives the test box in here as well.
/// </remarks>
internal sealed class DiagnosticsForm : Form
{
    private readonly Label _status = new();
    private readonly TextBox _details = new();
    private readonly TextBox _numericTest = new();
    private readonly TextBox _textTest = new();

    public DiagnosticsForm()
    {
        Text = "QweNumpad — Tanılama";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(620, 460);
        Padding = new Padding(12);
        MinimizeBox = false;

        _status.Dock = DockStyle.Top;
        _status.AutoSize = false;
        _status.Height = 28;
        _status.Font = new Font(SystemFonts.DefaultFont.FontFamily, 11, FontStyle.Bold);
        _status.Text = "Durum: —";

        _details.Dock = DockStyle.Fill;
        _details.Multiline = true;
        _details.ReadOnly = true;
        _details.ScrollBars = ScrollBars.Vertical;
        _details.Font = new Font(FontFamily.GenericMonospace, 9);
        _details.AccessibleName = "Tanılama çıktısı";

        // Deliberately named so the keyword heuristic classifies it as numeric.
        _numericTest.Name = "testNumericField";
        _numericTest.AccessibleName = "Tutar (test)";
        _numericTest.Dock = DockStyle.Top;
        _numericTest.Height = 26;

        // Deliberately named so the heuristic classifies it as text.
        _textTest.Name = "testTextField";
        _textTest.AccessibleName = "Açıklama (test)";
        _textTest.Dock = DockStyle.Top;
        _textTest.Height = 26;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(_status);
        layout.Controls.Add(Caption("Sayısal olarak algılanması gereken test alanı (\"Tutar (test)\"):"));
        layout.Controls.Add(_numericTest);
        layout.Controls.Add(Caption("Metin olarak algılanması gereken test alanı (\"Açıklama (test)\"):"));
        layout.Controls.Add(_textTest);
        layout.Controls.Add(_details);

        Controls.Add(layout);
    }

    private static Label Caption(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Padding = new Padding(0, 8, 0, 2),
    };

    /// <summary>Called on the UI thread whenever focus or mode changes.</summary>
    public void Update(FieldInfo? field, ModeState state, FieldHeuristics heuristics, LearnedFieldStore store)
    {
        _status.Text = $"Durum: {(state.IsActive ? "AÇIK — tuşlar numpad" : "kapalı — normal harfler")}" +
                       $"   |   Mod: {state.Override}";

        if (field is null)
        {
            _details.Text = "Odakta tanınan bir alan yok.";
            return;
        }

        var learned = store.Lookup(field.Signature);

        _details.Text = string.Join(Environment.NewLine, new[]
        {
            $"Uygulama       : {field.Signature.ProcessName}",
            $"Kontrol tipi   : {field.Signature.ControlType}",
            $"AutomationId   : {Or(field.Signature.AutomationId)}",
            $"Ad (Name)      : {Or(field.Signature.Name)}",
            $"HelpText       : {Or(field.HelpText)}",
            "",
            $"RangeValue     : {field.HasRangeValuePattern}",
            $"Spinner        : {field.IsSpinnerControlType}",
            $"ES_NUMBER      : {field.HasEsNumberStyle}",
            $"Parola alanı   : {field.IsPasswordField}",
            $"Salt okunur    : {field.IsReadOnly}",
            "",
            $"Sezgisel sonuç : {heuristics.LooksNumeric(field)}",
            $"Öğrenilmiş     : {(learned is null ? "kural yok" : learned.Value ? "SAYISAL" : "metin")}",
            $"KARAR          : {(learned ?? heuristics.LooksNumeric(field) ? "SAYISAL → tuşlar çevrilir" : "metin → çeviri yok")}",
            "",
            $"İmza anahtarı  : {field.Signature.Key}",
        });
    }

    private static string Or(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}
