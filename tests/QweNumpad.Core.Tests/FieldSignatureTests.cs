using QweNumpad.Core;

namespace QweNumpad.Core.Tests;

public class FieldSignatureTests
{
    private static FieldSignature Sig(string name, string automationId = "txtAmount") =>
        new("erp", "Edit", automationId, name);

    [Fact]
    public void Key_is_case_insensitive()
    {
        Assert.Equal(Sig("Tutar").Key, Sig("TUTAR").Key);
    }

    [Theory]
    [InlineData("Tutar: 150 TL", "Tutar: 2300 TL")]
    [InlineData("Satır 3 adet", "Satır 12 adet")]
    [InlineData("Fatura 2026-001", "Fatura 2026-047")]
    public void Labels_that_differ_only_by_numbers_share_a_key(string first, string second)
    {
        // Labels often carry the field's current value or a row number. If those digits
        // were part of the key, a rule the user taught would never match the same field again.
        Assert.Equal(Sig(first).Key, Sig(second).Key);
    }

    [Fact]
    public void Automation_ids_keep_their_digits()
    {
        // Win32 dialogs expose the control id as the automation id ("1001", "1002"),
        // and those ids are exactly what tells two fields apart.
        Assert.NotEqual(Sig("Tutar", "1001").Key, Sig("Tutar", "1002").Key);
    }

    [Fact]
    public void Whitespace_runs_in_labels_are_collapsed()
    {
        Assert.Equal(Sig("Tutar   TL").Key, Sig("Tutar TL").Key);
    }

    [Fact]
    public void Different_labels_still_differ()
    {
        Assert.NotEqual(Sig("Tutar").Key, Sig("Telefon").Key);
    }

    [Fact]
    public void Create_turns_nulls_into_empty_strings()
    {
        var signature = FieldSignature.Create(null, null, null, null);

        Assert.Equal("", signature.ProcessName);
        Assert.Equal("", signature.Name);
        Assert.Equal("|||", signature.Key);
    }
}
