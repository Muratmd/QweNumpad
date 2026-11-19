namespace QweNumpad.Core;

/// <summary>
/// Final verdict on whether the focused field should be remapped.
/// A rule the user taught always beats the heuristics.
/// </summary>
public sealed class FieldClassifier
{
    private readonly FieldHeuristics _heuristics;
    private readonly LearnedFieldStore _store;

    public FieldClassifier(FieldHeuristics heuristics, LearnedFieldStore store)
    {
        _heuristics = heuristics;
        _store = store;
    }

    public bool IsNumericField(FieldInfo field) =>
        _store.Lookup(field.Signature) ?? _heuristics.LooksNumeric(field);

    /// <summary>Records what the user meant for this field, overriding the heuristics from now on.</summary>
    public void Teach(FieldInfo field, bool numeric) => _store.Remember(field.Signature, numeric);
}
