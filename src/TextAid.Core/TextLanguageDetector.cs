using System.Text.RegularExpressions;

namespace TextAid.Core;

/// <summary>Performs a conservative local language guess for quick-translation routing.</summary>
public static class TextLanguageDetector
{
    private static readonly IReadOnlyDictionary<string, HashSet<string>> Signals = new Dictionary<string, HashSet<string>>
    {
        ["en"] = ["the", "and", "this", "that", "with", "from", "for", "please", "your", "have", "will", "text", "are", "not"],
        ["fr"] = ["le", "la", "les", "et", "ce", "cette", "avec", "pour", "dans", "vous", "est", "texte", "des", "une", "un", "du", "au", "quel", "quelle", "quels", "quelles"],
        ["es"] = ["el", "la", "los", "las", "y", "con", "para", "que", "texto", "una", "este", "por", "del"],
        ["it"] = ["il", "la", "gli", "le", "e", "con", "per", "che", "testo", "una", "questo", "del", "sono"],
        ["de"] = ["der", "die", "das", "und", "mit", "für", "diese", "diesen", "sie", "text", "eine", "einer", "ist", "nicht", "von", "den"],
        ["pt"] = ["o", "a", "os", "as", "e", "com", "para", "que", "texto", "uma", "este", "não", "dos"],
        ["nl"] = ["de", "het", "een", "en", "met", "voor", "deze", "tekst", "niet", "van", "zijn"],
        ["pl"] = ["i", "w", "z", "na", "to", "jest", "tekst", "dla", "nie", "oraz", "ten"]
    };

    /// <summary>Returns an ISO language tag only when local evidence is sufficient; otherwise returns null.</summary>
    public static string? Detect(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        string[] words = Regex.Matches(text.ToLowerInvariant(), "[\\p{L}']+").Select(match => match.Value).ToArray();
        if (words.Length < 5) return null;
        var scores = Signals.ToDictionary(pair => pair.Key, pair => words.Count(pair.Value.Contains));
        scores["fr"] += Regex.IsMatch(text, "[àâçéèêëîïôûùüÿœ]", RegexOptions.IgnoreCase) ? 2 : 0;
        scores["es"] += Regex.IsMatch(text, "[áéíóúüñ¡¿]", RegexOptions.IgnoreCase) ? 2 : 0;
        scores["de"] += Regex.IsMatch(text, "[äöüß]", RegexOptions.IgnoreCase) ? 2 : 0;
        scores["it"] += Regex.IsMatch(text, "[àèéìíîòóù]", RegexOptions.IgnoreCase) ? 1 : 0;
        scores["pt"] += Regex.IsMatch(text, "[ãõçáéíóú]", RegexOptions.IgnoreCase) ? 2 : 0;
        var ordered = scores.OrderByDescending(pair => pair.Value).ToArray();
        if (ordered[0].Value >= 3 && ordered[0].Value >= ordered[1].Value + 2) return ordered[0].Key;
        return null;
    }
}
