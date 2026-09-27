namespace TextAid.Core;

/// <summary>Describes how a requested model profile was selected or why it cannot be used.</summary>
public enum ProfileResolutionKind
{
    Selected,
    AutomaticallyDowngraded,
    UserConfirmationRequired,
    Failed
}

/// <summary>Contains a profile-resolution result and its user-facing status.</summary>
public sealed record ProfileResolution(
    ProfileResolutionKind Kind,
    ModelProfile? Profile,
    ConnectionDefinition? Connection,
    string Status,
    ConnectionCategory? RequestedCategory,
    ConnectionCategory? SelectedCategory);

/// <summary>Resolves action profiles while applying the TextAid downgrade policy.</summary>
public sealed class ProfileResolver
{
    private readonly ConfigurationSnapshot configuration;
    private readonly ISecretVault secretVault;

    /// <summary>Creates a resolver for one validated configuration snapshot.</summary>
    public ProfileResolver(ConfigurationSnapshot configuration, ISecretVault secretVault)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
    }

    /// <summary>Resolves a profile, automatically downgrading only missing or unconfigured categories.</summary>
    public ProfileResolution Resolve(string requestedProfileId)
    {
        ModelProfile? requestedProfile = configuration.Profiles.FirstOrDefault(profile => profile.Id.Equals(requestedProfileId, StringComparison.OrdinalIgnoreCase));
        if (requestedProfile is null)
            return Failure($"The requested model profile '{requestedProfileId}' does not exist.");

        ConnectionDefinition? requestedConnection = configuration.Connections.FirstOrDefault(connection => connection.Id.Equals(requestedProfile.ConnectionId, StringComparison.OrdinalIgnoreCase));
        if (requestedConnection is null)
            return ResolveMissingCategory(ConnectionCategory.ThisDeviceOnly, $"The requested profile '{requestedProfileId}' has no configured connection.");

        ValidationResult validation = Validate(requestedProfile, requestedConnection);
        if (validation.IsValid)
            return new ProfileResolution(ProfileResolutionKind.Selected, requestedProfile, requestedConnection, $"Using {Display(requestedConnection.Category)} configuration.", requestedConnection.Category, requestedConnection.Category);

        if (validation.IsNotConfigured)
            return ResolveMissingCategory(requestedConnection.Category, validation.Message);

        return OfferDowngrade(requestedConnection.Category, validation.Message);
    }

    /// <summary>Offers a user-controlled downgrade after an eligible provider failed during an invocation.</summary>
    public ProfileResolution OfferDowngradeAfterProviderFailure(ConnectionCategory failedCategory, string providerFailure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerFailure);
        return OfferDowngrade(failedCategory, providerFailure);
    }

    private ProfileResolution ResolveMissingCategory(ConnectionCategory requestedCategory, string reason)
    {
        Candidate? fallback = FindEligibleLowerCandidate(requestedCategory);
        if (fallback is null) return Failure($"{reason} No lower configuration is available.");
        return new ProfileResolution(ProfileResolutionKind.AutomaticallyDowngraded, fallback.Profile, fallback.Connection,
            $"{reason} Using {Display(fallback.Connection.Category)} configuration instead.", requestedCategory, fallback.Connection.Category);
    }

    private ProfileResolution OfferDowngrade(ConnectionCategory requestedCategory, string error)
    {
        Candidate? fallback = FindEligibleLowerCandidate(requestedCategory);
        if (fallback is null) return Failure($"{error} No lower configuration is available.");
        return new ProfileResolution(ProfileResolutionKind.UserConfirmationRequired, fallback.Profile, fallback.Connection,
            $"{error} Continue with {Display(fallback.Connection.Category)} configuration?", requestedCategory, fallback.Connection.Category);
    }

    private Candidate? FindEligibleLowerCandidate(ConnectionCategory category)
    {
        foreach (ConnectionCategory candidateCategory in LowerCategories(category))
        {
            foreach (ConnectionDefinition connection in configuration.Connections.Where(connection => connection.Category == candidateCategory))
            {
                foreach (ModelProfile profile in configuration.Profiles.Where(profile => profile.ConnectionId.Equals(connection.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    if (Validate(profile, connection).IsValid) return new Candidate(profile, connection);
                }
            }
        }

        return null;
    }

    private ValidationResult Validate(ModelProfile profile, ConnectionDefinition connection)
    {
        if (!connection.IsEnabled)
            return new ValidationResult(false, true, $"{Display(connection.Category)} configuration is inactive.");
        if (string.IsNullOrWhiteSpace(connection.Endpoint) || string.IsNullOrWhiteSpace(profile.Model))
            return new ValidationResult(false, true, $"{Display(connection.Category)} configuration is not configured.");
        if (!Uri.TryCreate(connection.Endpoint, UriKind.Absolute, out Uri? endpoint) || endpoint.Scheme is not "http" and not "https")
            return new ValidationResult(false, false, $"{Display(connection.Category)} configuration has an invalid endpoint.");
        if (connection.Category == ConnectionCategory.ThisDeviceOnly && !UserConfiguration.IsLoopbackHost(endpoint.Host))
            return new ValidationResult(false, false, "This device only configuration must use a loopback endpoint.");
        if (profile.Temperature is < 0 or > 2 || profile.Timeout <= TimeSpan.Zero)
            return new ValidationResult(false, false, $"{Display(connection.Category)} configuration has invalid generation settings.");
        if (connection.Authentication == AuthenticationKind.ApiKey && (string.IsNullOrWhiteSpace(connection.SecretReference) || !secretVault.TryGetSecret(connection.SecretReference, out _)))
            return new ValidationResult(false, false, $"{Display(connection.Category)} configuration requires a readable secret.");
        return new ValidationResult(true, false, string.Empty);
    }

    private static IEnumerable<ConnectionCategory> LowerCategories(ConnectionCategory category) => category switch
    {
        ConnectionCategory.External => [ConnectionCategory.OnPremises, ConnectionCategory.ThisDeviceOnly],
        ConnectionCategory.OnPremises => [ConnectionCategory.ThisDeviceOnly],
        _ => []
    };

    private static string Display(ConnectionCategory category) => category switch
    {
        ConnectionCategory.ThisDeviceOnly => "This device only",
        ConnectionCategory.OnPremises => "On-premises",
        _ => "External"
    };

    private static ProfileResolution Failure(string status) => new(ProfileResolutionKind.Failed, null, null, status, null, null);

    private sealed record Candidate(ModelProfile Profile, ConnectionDefinition Connection);
    private sealed record ValidationResult(bool IsValid, bool IsNotConfigured, string Message);
}
