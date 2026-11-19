using QweNumpad.Core;

namespace QweNumpad.Core.Tests;

public class FieldHeuristicsTests
{
    private readonly FieldHeuristics _heuristics = new();

    private static FieldInfo Field(
        string name = "",
        string automationId = "",
        string controlType = "Edit",
        bool rangeValue = false,
        bool spinner = false,
        bool esNumber = false,
        string? helpText = null,
        string? placeholder = null,
        bool password = false,
        bool readOnly = false) =>
        new(new FieldSignature("app", controlType, automationId, name),
            rangeValue, spinner, esNumber, helpText, placeholder, password, readOnly);

    [Fact]
    public void Range_value_pattern_means_numeric() =>
        Assert.True(_heuristics.LooksNumeric(Field(rangeValue: true)));

    [Fact]
    public void Spinner_control_type_means_numeric() =>
        Assert.True(_heuristics.LooksNumeric(Field(spinner: true)));

    [Fact]
    public void Win32_es_number_style_means_numeric() =>
        Assert.True(_heuristics.LooksNumeric(Field(esNumber: true)));

    [Theory]
    [InlineData("Telefon")]
    [InlineData("txtTelefonNo")]
    [InlineData("Kart Numarası")]
    [InlineData("PhoneNumber")]
    [InlineData("phone-number")]
    [InlineData("IBAN")]
    [InlineData("Tutar")]
    [InlineData("amount_due")]
    [InlineData("Posta Kodu")]
    [InlineData("TC Kimlik")]
    [InlineData("otp")]
    [InlineData("Adet")]
    [InlineData("Fatura No")]
    public void Numeric_sounding_names_are_numeric(string name) =>
        Assert.True(_heuristics.LooksNumeric(Field(name: name)), name);

    [Theory]
    [InlineData("Notes")]
    [InlineData("Nothing")]
    [InlineData("Country")]
    [InlineData("Address")]
    [InlineData("Ad Soyad")]
    [InlineData("Search")]
    [InlineData("Arama")]
    [InlineData("Message body")]
    [InlineData("")]
    public void Text_sounding_names_are_not_numeric(string name) =>
        Assert.False(_heuristics.LooksNumeric(Field(name: name)), name);

    [Fact]
    public void Placeholder_and_help_text_are_considered()
    {
        Assert.True(_heuristics.LooksNumeric(Field(placeholder: "05XX telefon numarası")));
        Assert.True(_heuristics.LooksNumeric(Field(helpText: "Enter the amount")));
    }

    [Fact]
    public void Password_fields_are_never_remapped() =>
        Assert.False(_heuristics.LooksNumeric(Field(name: "PIN", esNumber: true, password: true)));

    [Fact]
    public void Read_only_fields_are_never_remapped() =>
        Assert.False(_heuristics.LooksNumeric(Field(name: "Tutar", readOnly: true)));

    [Fact]
    public void Extra_keywords_extend_detection()
    {
        var custom = new FieldHeuristics(new[] { "sicil" });

        Assert.True(custom.LooksNumeric(Field(name: "Sicil")));
        Assert.False(_heuristics.LooksNumeric(Field(name: "Sicil")));
    }

    [Fact]
    public void Non_editable_control_types_are_not_numeric() =>
        Assert.False(_heuristics.LooksNumeric(Field(name: "Tutar", controlType: "Button")));
}
