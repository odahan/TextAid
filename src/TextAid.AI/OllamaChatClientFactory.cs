using Microsoft.Extensions.AI;
using OllamaSharp;
using TextAid.Core;

namespace TextAid.AI;

/// <summary>Creates the application's only local-provider client boundary through OllamaSharp.</summary>
public sealed class OllamaChatClientFactory
{
    /// <summary>Creates an IChatClient for the configured local endpoint and model.</summary>
    public IChatClient Create(TextTransformationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return (IChatClient)new OllamaApiClient(settings.Endpoint, settings.Model);
    }

    /// <summary>Retrieves model names installed at the configured local Ollama endpoint.</summary>
    public async Task<IReadOnlyList<string>> GetLocalModelNamesAsync(string endpoint, CancellationToken cancellationToken)
    {
        using var client = new OllamaApiClient(endpoint, string.Empty);
        IEnumerable<OllamaSharp.Models.Model> models = await client.ListLocalModelsAsync(cancellationToken);
        return models
            .Select(model => model.Name ?? model.ModelName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray()!;
    }

    /// <summary>Checks whether the configured Ollama server is responding.</summary>
    public async Task<bool> TestConnectionAsync(string endpoint, CancellationToken cancellationToken)
    {
        using var client = new OllamaApiClient(endpoint, string.Empty);
        return await client.IsRunningAsync(cancellationToken);
    }
}
