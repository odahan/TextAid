namespace TextAid.Core;

/// <summary>Describes how a requested model profile was selected or why it cannot be used.</summary>
public enum ProfileResolutionKind
{
    Selected,
    AutomaticallyDowngraded,
    AutomaticallyPromoted,
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
    public ProfileResolution Resolve(string requestedProfileId, bool exact = false)
    {
        ModelProfile? requestedProfile = configuration.Profiles.FirstOrDefault(profile => profile.Id.Equals(requestedProfileId, StringComparison.OrdinalIgnoreCase));
        if (requestedProfile is null)
            return Failure($"The requested model profile '{requestedProfileId}' does not exist.");

        ConnectionDefinition? requestedConnection = configuration.Connections.FirstOrDefault(connection => connection.Id.Equals(requestedProfile.ConnectionId, StringComparison.OrdinalIgnoreCase));
        if (requestedConnection is null)
        {
            if (exact) return Failure($"The requested profile '{requestedProfileId}' has no configured connection.");
            return ResolveMissingCategory(ConnectionCategory.ThisDeviceOnly, $"The requested profile '{requestedProfileId}' has no configured connection.");
        }

        ValidationResult validation = Validate(requestedProfile, requestedConnection);
        if (exact)
            return validation.IsValid
                ? new(ProfileResolutionKind.Selected, requestedProfile, requestedConnection, $"Using {Display(requestedConnection.Category)} configuration.", requestedConnection.Category, requestedConnection.Category)
                : Failure(validation.Message);
        Candidate? configuredAccount = FindAccountCandidate();
        bool onlyExternalConfigured = !configuration.Connections.Any(connection => connection.IsEnabled && connection.Category != ConnectionCategory.External &&
            configuration.Profiles.Any(profile => profile.ConnectionId == connection.Id && Validate(profile, connection).IsValid));
        if (configuredAccount is not null && onlyExternalConfigured && (validation.IsValid || validation.IsNotConfigured))
        {
            ValidationResult accountValidation = Validate(configuredAccount.Profile, configuredAccount.Connection);
            if (!accountValidation.IsValid) return Failure(accountValidation.Message);
        }
        if (requestedConnection.Category == ConnectionCategory.External && requestedConnection.Provider != "openai-account")
        {
            Candidate? preferred = FindAccountCandidate();
            if (preferred is not null)
            {
                ValidationResult accountValidation = Validate(preferred.Profile, preferred.Connection);
                if (!accountValidation.IsValid) return OfferDowngrade(ConnectionCategory.External, accountValidation.Message);
                requestedProfile = preferred.Profile;
                requestedConnection = preferred.Connection;
                validation = accountValidation;
            }
        }
        if (requestedConnection.Provider == "openai-account" && validation.IsNotConfigured)
        {
            Candidate? peer = FindEligibleConnectionCandidates().FirstOrDefault(candidate => candidate.Connection.Category == ConnectionCategory.External);
            if (peer is not null)
            {
                requestedProfile = peer.Profile;
                requestedConnection = peer.Connection;
                validation = Validate(requestedProfile, requestedConnection);
            }
        }
        IReadOnlyList<Candidate> eligibleConnections = FindEligibleConnectionCandidates();
        if (eligibleConnections.Count == 1 && (validation.IsValid || validation.IsNotConfigured))
        {
            Candidate soleCandidate = eligibleConnections[0];
            if (validation.IsValid && soleCandidate.Connection.Id.Equals(requestedConnection.Id, StringComparison.OrdinalIgnoreCase) && soleCandidate.Profile.Id.Equals(requestedProfile.Id, StringComparison.OrdinalIgnoreCase))
                return new ProfileResolution(ProfileResolutionKind.Selected, requestedProfile, requestedConnection, $"Using {Display(requestedConnection.Category)} configuration.", requestedConnection.Category, requestedConnection.Category);

            ProfileResolutionKind kind = soleCandidate.Connection.Category > requestedConnection.Category
                ? ProfileResolutionKind.AutomaticallyPromoted
                : ProfileResolutionKind.AutomaticallyDowngraded;
            string reason = validation.IsValid
                ? $"The action requested {Display(requestedConnection.Category)} configuration."
                : $"The requested {Display(requestedConnection.Category)} configuration is unavailable: {validation.Message}";
            return new ProfileResolution(kind, soleCandidate.Profile, soleCandidate.Connection,
                $"{reason} Using the only active {Display(soleCandidate.Connection.Category)} configuration instead.",
                requestedConnection.Category, soleCandidate.Connection.Category);
        }

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
        if (fallback is null)
        {
            if (FindEligibleConnectionCandidates().Count > 1)
                return Failure($"{reason} Multiple active configurations are available. Select an appropriate profile for this action in Settings.");
            return Failure($"{reason} No eligible configuration is available. Review the connection and model settings.");
        }
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

    private IReadOnlyList<Candidate> FindEligibleConnectionCandidates()
    {
        var candidates = new List<Candidate>();
        foreach (ConnectionDefinition connection in configuration.Connections)
        {
            ModelProfile? profile = configuration.Profiles.FirstOrDefault(profile =>
                profile.ConnectionId.Equals(connection.Id, StringComparison.OrdinalIgnoreCase) && Validate(profile, connection).IsValid);
            if (profile is not null) candidates.Add(new Candidate(profile, connection));
        }

        if (candidates.Any(candidate => candidate.Connection.Provider == "openai-account"))
            candidates.RemoveAll(candidate => candidate.Connection.Category == ConnectionCategory.External && candidate.Connection.Provider != "openai-account");
        return candidates;
    }

    private Candidate? FindAccountCandidate()
    {
        ConnectionDefinition? connection = configuration.Connections.FirstOrDefault(connection => connection.Provider == "openai-account" && connection.IsEnabled);
        ModelProfile? profile = connection is null ? null : configuration.Profiles.FirstOrDefault(profile => profile.ConnectionId == connection.Id && !string.IsNullOrWhiteSpace(profile.Model));
        return profile is null ? null : new(profile, connection!);
    }

    /// <summary>Lists structurally configured profiles without probing provider availability.</summary>
    public IReadOnlyList<ModelProfile> GetSelectableProfiles() => configuration.Profiles.Where(profile =>
    {
        ConnectionDefinition? connection = configuration.Connections.FirstOrDefault(connection => connection.Id == profile.ConnectionId);
        return connection is not null && Validate(profile, connection).IsValid;
    }).ToArray();

    private ValidationResult Validate(ModelProfile profile, ConnectionDefinition connection)
    {
        if (connection.Provider is not ("ollama" or "openai-compatible" or "openai-account"))
            return new(false, false, "OpenAiUnsupportedOptions");
        if (connection.Provider != "openai-account" && (connection.Authentication == AuthenticationKind.OpenAiAccount || connection.SecretReference?.StartsWith("openai-account-", StringComparison.OrdinalIgnoreCase) == true))
            return new(false, false, "OpenAiReconnect");
        if (!connection.IsEnabled)
            return new ValidationResult(false, true, $"{Display(connection.Category)} configuration is inactive.");
        if (string.IsNullOrWhiteSpace(connection.Endpoint) || string.IsNullOrWhiteSpace(profile.Model))
            return new ValidationResult(false, true, $"{Display(connection.Category)} configuration is not configured.");
        if (!Uri.TryCreate(connection.Endpoint, UriKind.Absolute, out Uri? endpoint) || endpoint.Scheme is not "http" and not "https")
            return new ValidationResult(false, false, $"{Display(connection.Category)} configuration has an invalid endpoint.");
        if (connection.Category == ConnectionCategory.External && !UserConfiguration.IsLoopbackHost(endpoint.Host) && endpoint.Scheme != "https")
            return new ValidationResult(false, false, "External configuration requires HTTPS outside this device.");
        if (connection.Provider.Equals("openai-compatible", StringComparison.OrdinalIgnoreCase) && connection.Category != ConnectionCategory.External)
            return new ValidationResult(false, false, "OpenAI-compatible configuration requires the External category.");
        if (connection.Category == ConnectionCategory.ThisDeviceOnly && !UserConfiguration.IsLoopbackHost(endpoint.Host))
            return new ValidationResult(false, false, "This device only configuration must use a loopback endpoint.");
        if (profile.Temperature is < 0 or > 2 || profile.Timeout <= TimeSpan.Zero)
            return new ValidationResult(false, false, $"{Display(connection.Category)} configuration has invalid generation settings.");
        if (connection.Provider == "openai-account")
        {
            if (connection.Category != ConnectionCategory.External || connection.Endpoint != UserConfiguration.DefaultExternalEndpoint || connection.Authentication != AuthenticationKind.OpenAiAccount)
                return new(false, false, "OpenAiReconnect");
            if (string.IsNullOrWhiteSpace(connection.SecretReference) || !secretVault.TryGetSecret(connection.SecretReference, out string session))
                return new(false, false, "OpenAiReconnect");
            try
            {
                using var json = System.Text.Json.JsonDocument.Parse(session);
                if (!json.RootElement.TryGetProperty("access_token", out var access) || string.IsNullOrWhiteSpace(access.GetString()) ||
                    !json.RootElement.TryGetProperty("scope", out var scope) || !(scope.GetString() ?? "").Split(' ').Contains("chatgpt.tokens.use.direct"))
                    return new(false, false, "OpenAiPermissionMissing");
            }
            catch { return new(false, false, "OpenAiReconnect"); }
        }
        if (connection.Authentication == AuthenticationKind.ApiKey && (string.IsNullOrWhiteSpace(connection.SecretReference) || !secretVault.TryGetSecret(connection.SecretReference, out _)))
            return new ValidationResult(false, false, $"{Display(connection.Category)} configuration requires a readable secret.");
        if (connection.Authentication == AuthenticationKind.BearerFromEnvironment)
            return new ValidationResult(false, false, $"{Display(connection.Category)} configuration must use a protected DPAPI credential.");
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
