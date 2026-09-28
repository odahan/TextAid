using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Models;
using System.ClientModel;
using System.ClientModel.Primitives;
using TextAid.Core;

namespace TextAid.AI;

/// <summary>Creates OpenAI-compatible IChatClient instances without bypassing Microsoft.Extensions.AI.</summary>
public sealed class OpenAiCompatibleChatClientFactory
{
    private static readonly TimeSpan ExternalNetworkTimeout = TimeSpan.FromMinutes(10);
    private readonly ISecretVault secretVault;

    /// <summary>Creates the factory with the current user's protected secret store.</summary>
    public OpenAiCompatibleChatClientFactory() : this(new DpapiSecretVault())
    {
    }

    /// <summary>Creates the factory with an explicit secret store for deterministic configuration handling.</summary>
    public OpenAiCompatibleChatClientFactory(ISecretVault secretVault)
    {
        this.secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
    }

    /// <summary>Creates a configured OpenAI-compatible client using the configured protected credential.</summary>
    public IChatClient Create(ConnectionDefinition connection, ModelProfile profile)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(profile);
        return CreateClient(connection, null).GetChatClient(profile.Model).AsIChatClient();
    }

    /// <summary>Retrieves the models visible to the configured External connection without persisting a newly typed key.</summary>
    public async Task<IReadOnlyList<string>> GetModelNamesAsync(ConnectionDefinition connection, string? suppliedApiKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        OpenAIModelCollection models = await CreateClient(connection, suppliedApiKey).GetOpenAIModelClient().GetModelsAsync(cancellationToken);
        return models
            .Select(model => model.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private OpenAIClient CreateClient(ConnectionDefinition connection, string? suppliedApiKey)
    {
        if (!Uri.TryCreate(connection.Endpoint, UriKind.Absolute, out Uri? endpoint)) throw new InvalidOperationException("The External endpoint is invalid.");
        string? apiKey = connection.Authentication switch
        {
            AuthenticationKind.None => null,
            AuthenticationKind.ApiKey => string.IsNullOrWhiteSpace(suppliedApiKey) ? ResolveProtectedSecret(connection.SecretReference) : suppliedApiKey,
            _ => throw new NotSupportedException("OpenAI-compatible connections require None or a protected DPAPI credential.")
        };
        var options = new OpenAIClientOptions
        {
            Endpoint = endpoint,
            NetworkTimeout = ExternalNetworkTimeout
        };
#pragma warning disable OPENAI001 // The OpenAI SDK exposes no stable anonymous-client constructor.
        return apiKey is null
            ? new OpenAIClient(new NoAuthenticationPolicy(), options)
            : new OpenAIClient(new ApiKeyCredential(apiKey), options);
#pragma warning restore OPENAI001
    }

    private string ResolveProtectedSecret(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || !secretVault.TryGetSecret(reference, out string secret))
            throw new InvalidOperationException("The External protected credential is not available.");
        return secret;
    }

    private sealed class NoAuthenticationPolicy : AuthenticationPolicy
    {
        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex) =>
            ProcessNext(message, pipeline, currentIndex);

        public override ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex) =>
            ProcessNextAsync(message, pipeline, currentIndex);
    }
}
