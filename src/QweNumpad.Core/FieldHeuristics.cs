using System.Globalization;
using System.Text;

namespace QweNumpad.Core;

/// <summary>
/// Guesses whether a focused field wants digits. Strong signals (UI Automation patterns,
/// Win32 ES_NUMBER) win outright; otherwise the field's name/id/placeholder is matched
/// against a keyword list. Wrong guesses are expected and are corrected by the user's
/// override hotkey, which <see cref="LearnedFieldStore"/> then remembers.
/// </summary>
public sealed class FieldHeuristics
{
    /// <summary>
    /// Control types that can receive typed text. Anything else (buttons, lists, labels)
    /// is never remapped, so a stray keyword in a button's label cannot trigger the mode.
    /// </summary>
    private static readonly HashSet<string> EditableControlTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "edit", "document", "combobox", "spinner", "custom",
    };

    /// <summary>
    /// Matched against whole tokens only. Short words would produce too many false
    /// positives as substrings ("no" inside "Notes").
    /// </summary>
    private static readonly string[] ShortKeywords =
    {
        "no", "tc", "tel", "gsm", "otp", "pin", "vkn", "zip", "qty", "num", "kod", "yil",
    };

    /// <summary>Matched as substrings anywhere in the normalized text.</summary>
    private static readonly string[] LongKeywords =
    {
        "telefon", "phone", "mobile", "numara", "number", "tutar", "miktar", "fiyat",
        "price", "amount", "iban", "kart", "card", "kredi", "kimlik", "fatura", "posta",
        "postcode", "zipcode", "adet", "quantity", "year", "hesap", "account", "barkod",
        "barcode", "taksit", "vade", "yuzde", "percent", "kodu", "tutari", "bakiye",
        "balance", "invoice", "salary", "maas", "puan", "score",
    };

    private readonly string[] _extraKeywords;

    public FieldHeuristics(IEnumerable<string>? extraKeywords = null) =>
        _extraKeywords = (extraKeywords ?? Enumerable.Empty<string>())
            .Select(k => Normalize(k).ToLowerInvariant().Trim())
            .Where(k => k.Length > 0)
            .ToArray();

    public static IReadOnlyList<string> DefaultKeywords { get; } =
        ShortKeywords.Concat(LongKeywords).ToArray();

    public bool LooksNumeric(FieldInfo field)
    {
        // Never touch a password box: swallowing keys there is both useless and alarming.
        if (field.IsPasswordField || field.IsReadOnly) return false;

        if (!field.IsSpinnerControlType && !EditableControlTypes.Contains(field.Signature.ControlType))
            return false;

        if (field.HasRangeValuePattern || field.IsSpinnerControlType || field.HasEsNumberStyle)
            return true;

        return MatchesKeyword(field.Signature.Name)
            || MatchesKeyword(field.Signature.AutomationId)
            || MatchesKeyword(field.HelpText)
            || MatchesKeyword(field.Placeholder);
    }

    private bool MatchesKeyword(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Case is preserved for tokenizing (camelCase boundaries) and folded for substrings.
        var normalized = Normalize(text);
        if (normalized.Length == 0) return false;
        var lowered = normalized.ToLowerInvariant();

        foreach (var keyword in LongKeywords)
            if (lowered.Contains(keyword, StringComparison.Ordinal)) return true;

        foreach (var keyword in _extraKeywords)
        {
            if (keyword.Length <= 3)
            {
                if (HasToken(normalized, keyword)) return true;
            }
            else if (lowered.Contains(keyword, StringComparison.Ordinal))
            {
                return true;
            }
        }

        foreach (var keyword in ShortKeywords)
            if (HasToken(normalized, keyword)) return true;

        return false;
    }

    private static bool HasToken(string normalized, string keyword)
    {
        foreach (var token in Tokenize(normalized))
            if (token.Equals(keyword, StringComparison.Ordinal)) return true;

        return false;
    }

    /// <summary>
    /// Splits identifiers and labels into lowercase words, handling camelCase, acronym
    /// runs and separators: "txtTelefonNo" -> txt, telefon, no; "IBANNo" -> iban, no.
    /// </summary>
    private static IEnumerable<string> Tokenize(string normalized)
    {
        var current = new StringBuilder();

        for (var i = 0; i < normalized.Length; i++)
        {
            var c = normalized[i];

            if (!char.IsLetterOrDigit(c))
            {
                if (current.Length > 0) { yield return current.ToString(); current.Clear(); }
                continue;
            }

            if (char.IsUpper(c) && current.Length > 0)
            {
                var prev = normalized[i - 1];
                var nextIsLower = i + 1 < normalized.Length && char.IsLower(normalized[i + 1]);

                if (char.IsLower(prev) || char.IsDigit(prev) || (char.IsUpper(prev) && nextIsLower))
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }

            current.Append(char.ToLowerInvariant(c));
        }

        if (current.Length > 0) yield return current.ToString();
    }

    /// <summary>
    /// Folds Turkish (and other) diacritics to ASCII so one keyword list covers
    /// "Numarası" and "Numarasi", "Yıl" and "Yil".
    /// </summary>
    private static string Normalize(string text)
    {
        var replaced = text
            .Replace('ı', 'i').Replace('İ', 'I')
            .Replace('ş', 's').Replace('Ş', 'S')
            .Replace('ğ', 'g').Replace('Ğ', 'G')
            .Replace('ü', 'u').Replace('Ü', 'U')
            .Replace('ö', 'o').Replace('Ö', 'O')
            .Replace('ç', 'c').Replace('Ç', 'C');

        var decomposed = replaced.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);

        return builder.ToString();
    }
}
