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

    /// <summary>Determines whether at least one enabled provider has an endpoint and a selected model.</summary>
    public bool HasEnabledConfiguredProvider() => Connections.Any(connection =>
        connection.IsEnabled
        && !string.IsNullOrWhiteSpace(connection.Endpoint)
        && Profiles.Any(profile =>
            profile.ConnectionId.Equals(connection.Id, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(profile.Model)));
}
