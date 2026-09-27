using Microsoft.Extensions.AI;
using TextAid.Core;

namespace TextAid.AI;

/// <summary>Creates provider clients from resolved connection and model-profile definitions.</summary>
public sealed class AiClientFactory
{
    private readonly OllamaChatClientFactory ollamaFactory = new();

    /// <summary>Creates a client for a validated, resolved connection.</summary>
    public IChatClient Create(ConnectionDefinition connection, ModelProfile profile)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(profile);
        if (!connection.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"The '{connection.Provider}' provider is not available until LOT-009.");

        int contextSize = profile.ProviderOptions.TryGetValue("num_ctx", out object? value) && int.TryParse(value?.ToString(), out int parsed) ? parsed : 8192;
        ThinkingMode thinking = profile.ProviderOptions.TryGetValue("think", out object? think) && Enum.TryParse(think?.ToString(), true, out ThinkingMode parsedThinking) ? parsedThinking : ThinkingMode.Off;
        return ollamaFactory.Create(new TextTransformationSettings(connection.Endpoint, profile.Model, profile.Temperature, profile.Timeout, contextSize, thinking));
    }
}
