namespace TextAid.Core;

/// <summary>Represents the validated versioned configuration used by the application.</summary>
public sealed record ConfigurationSnapshot(
    int SchemaVersion,
    IReadOnlyList<ConnectionDefinition> Connections,
    IReadOnlyList<ModelProfile> Profiles,
    string UserLanguage,
    string PreferredTranslationLanguage,
    string NormalShortcut,
    string TranslationShortcut)
{
    /// <summary>Returns the profile with the stable local default identifier.</summary>
    public ModelProfile LocalDefaultProfile => Profiles.Single(profile => profile.Id.Equals("local-default", StringComparison.OrdinalIgnoreCase));
}
