namespace TextAid.Core;

/// <summary>Identifies the deployment boundary of an AI connection.</summary>
public enum ConnectionCategory
{
    ThisDeviceOnly,
    OnPremises,
    External
}

/// <summary>Describes how a connection authenticates to its provider.</summary>
public enum AuthenticationKind
{
    None,
    /// <summary>Represents an earlier environment-based configuration and is rejected for new External connections.</summary>
    BearerFromEnvironment,
    /// <summary>Retains the DPAPI-backed credential option introduced for existing user configurations.</summary>
    ApiKey
}

/// <summary>Describes one configured provider endpoint without retaining its secret.</summary>
public sealed record ConnectionDefinition(
    string Id,
    ConnectionCategory Category,
    string Provider,
    string Endpoint,
    bool IsEnabled,
    AuthenticationKind Authentication,
    string? SecretReference);
