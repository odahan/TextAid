namespace TextAid.Core;

/// <summary>Describes a model and generation settings assigned to a connection.</summary>
public sealed record ModelProfile(
    string Id,
    string ConnectionId,
    string Model,
    float Temperature,
    TimeSpan Timeout,
    IReadOnlyDictionary<string, object?> ProviderOptions);
