using QweNumpad.Core;

namespace QweNumpad.Core.Tests;

public class LearnedFieldStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "qwenumpad-tests-" + Guid.NewGuid().ToString("N"));
    private string Path_ => Path.Combine(_dir, "learned.json");

    private static FieldSignature Sig(string name = "Tutar") => new("notepad", "Edit", "txt1", name);

    [Fact]
    public void Unknown_signature_returns_null()
    {
        var store = new LearnedFieldStore(Path_);

        Assert.Null(store.Lookup(Sig()));
    }

    [Fact]
    public void Remembers_positive_and_negative_rules()
    {
        var store = new LearnedFieldStore(Path_);

        store.Remember(Sig("a"), true);
        store.Remember(Sig("b"), false);

        Assert.True(store.Lookup(Sig("a")));
        Assert.False(store.Lookup(Sig("b")));
    }

    [Fact]
    public void Survives_reload()
    {
        var store = new LearnedFieldStore(Path_);
        store.Remember(Sig(), true);
        store.Save();

        var reloaded = new LearnedFieldStore(Path_);

        Assert.True(reloaded.Lookup(Sig()));
    }

    [Fact]
    public void Forget_removes_a_single_rule()
    {
        var store = new LearnedFieldStore(Path_);
        store.Remember(Sig("a"), true);
        store.Remember(Sig("b"), true);

        store.Forget(Sig("a"));

        Assert.Null(store.Lookup(Sig("a")));
        Assert.True(store.Lookup(Sig("b")));
    }

    [Fact]
    public void Clear_removes_everything()
    {
        var store = new LearnedFieldStore(Path_);
        store.Remember(Sig(), true);

        store.Clear();

        Assert.Null(store.Lookup(Sig()));
        Assert.Empty(store.Entries);
    }

    [Fact]
    public void Corrupt_file_is_ignored_instead_of_crashing()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path_, "{ this is not json");

        var store = new LearnedFieldStore(Path_);

        Assert.Empty(store.Entries);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }
}
