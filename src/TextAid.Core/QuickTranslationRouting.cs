namespace TextAid.Core;

/// <summary>Chooses the translation destination for the immediate invocation route.</summary>
public static class QuickTranslationRouting
{
    /// <summary>Returns the appropriate destination for a detected source language.</summary>
    public static string SelectDestination(string? detectedLanguage, string userLanguage, string preferredTranslationLanguage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userLanguage);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredTranslationLanguage);
        if (Matches(detectedLanguage, userLanguage)) return preferredTranslationLanguage;
        if (Matches(detectedLanguage, preferredTranslationLanguage)) return userLanguage;
        return userLanguage;
    }

    private static bool Matches(string? first, string second) =>
        !string.IsNullOrWhiteSpace(first) &&
        (first.Equals(second, StringComparison.OrdinalIgnoreCase) ||
         first.StartsWith(second + "-", StringComparison.OrdinalIgnoreCase) ||
         second.StartsWith(first + "-", StringComparison.OrdinalIgnoreCase));
}
