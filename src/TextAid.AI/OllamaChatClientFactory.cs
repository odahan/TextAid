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

    /// <summary>Determines whether the selected model is currently held in the Ollama server's memory.</summary>
    public async Task<bool> IsModelLoadedAsync(TextTransformationSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        using var client = new OllamaApiClient(settings.Endpoint, settings.Model);
        IEnumerable<OllamaSharp.Models.RunningModel> models = await client.ListRunningModelsAsync(cancellationToken);
        return models.Any(model => IsSameModel(model.Name ?? model.ModelName, settings.Model));
    }

    /// <summary>Loads a selected model without generating visible user content.</summary>
    public async Task WarmupAsync(TextTransformationSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        using var client = new OllamaApiClient(settings.Endpoint, settings.Model);
        var request = new OllamaSharp.Models.GenerateRequest
        {
            Model = settings.Model,
            Prompt = string.Empty,
            KeepAlive = "5m",
            Options = new OllamaSharp.Models.RequestOptions { NumCtx = settings.ContextSize }
        };

        await foreach (var _ in client.GenerateAsync(request, cancellationToken))
        {
            // Enumerating the completion waits until Ollama has loaded the model.
        }
    }

    private static bool IsSameModel(string? loadedModel, string selectedModel)
    {
        if (string.Equals(loadedModel, selectedModel, StringComparison.OrdinalIgnoreCase)) return true;
        return string.Equals(AppendDefaultTag(loadedModel), AppendDefaultTag(selectedModel), StringComparison.OrdinalIgnoreCase);
    }

    private static string AppendDefaultTag(string? model) =>
        string.IsNullOrWhiteSpace(model) || model.Contains(':', StringComparison.Ordinal) ? model ?? string.Empty : $"{model}:latest";
}
