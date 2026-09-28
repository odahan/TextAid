using System.Globalization;

namespace TextAid.Core;

/// <summary>Provides the supported UI and translation languages with stable BCP-47 identifiers.</summary>
public static class LanguageCatalog
{
    private static readonly IReadOnlyList<LanguageOption> supported =
    [
        Create("en"), Create("en-US"), Create("fr"), Create("fr-FR"), Create("de"), Create("es"), Create("it"),
        Create("pt"), Create("nl"), Create("pl"), Create("uk"), Create("ru"),
        Create("ja"), Create("ko"), Create("zh-Hans"), Create("ar")
    ];

    /// <summary>Gets all languages offered by TextAid.</summary>
    public static IReadOnlyList<LanguageOption> Supported => supported;

    /// <summary>Determines whether a language is in the TextAid catalog.</summary>
    public static bool IsSupported(string? language) => supported.Any(option => option.Code.Equals(language, StringComparison.OrdinalIgnoreCase));

    private static LanguageOption Create(string code)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(code);
        return new LanguageOption(code, culture.EnglishName, culture.NativeName);
    }
}

/// <summary>Describes one language that can be selected in TextAid.</summary>
public sealed record LanguageOption(string Code, string EnglishName, string NativeName, string? DisplayNameOverride = null)
{
    /// <summary>Gets the readable label displayed by English source UI.</summary>
    public string DisplayName => DisplayNameOverride ?? $"{EnglishName} ({NativeName})";

    /// <summary>Uses the readable label when WPF renders this value without an item template.</summary>
    public override string ToString() => DisplayName;
}
