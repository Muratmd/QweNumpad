using QweNumpad.Core;

namespace QweNumpad.Core.Tests;

public class FieldClassifierTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "qwenumpad-cls-" + Guid.NewGuid().ToString("N"));
    private readonly LearnedFieldStore _store;
    private readonly FieldClassifier _classifier;

    public FieldClassifierTests()
    {
        _store = new LearnedFieldStore(Path.Combine(_dir, "learned.json"));
        _classifier = new FieldClassifier(new FieldHeuristics(), _store);
    }

    private static FieldInfo Field(string name) =>
        new(new FieldSignature("app", "Edit", "id", name), false, false, false);

    [Fact]
    public void Falls_back_to_heuristics_when_nothing_learned()
    {
        Assert.True(_classifier.IsNumericField(Field("Telefon")));
        Assert.False(_classifier.IsNumericField(Field("Notes")));
    }

    [Fact]
    public void Learned_negative_overrides_positive_heuristic()
    {
        var field = Field("Telefon");
        _store.Remember(field.Signature, false);

        Assert.False(_classifier.IsNumericField(field));
    }

    [Fact]
    public void Learned_positive_overrides_negative_heuristic()
    {
        var field = Field("Notes");
        _store.Remember(field.Signature, true);

        Assert.True(_classifier.IsNumericField(field));
    }

    [Fact]
    public void Teaching_records_the_opposite_of_the_current_verdict()
    {
        var field = Field("Notes");

        _classifier.Teach(field, numeric: true);

        Assert.True(_classifier.IsNumericField(field));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }
}
