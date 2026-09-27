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
