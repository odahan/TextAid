using Microsoft.Extensions.AI;
using TextAid.AI;
using TextAid.Core;

namespace TextAid.AI.Tests;

public sealed class OpenAiCompatibleChatClientFactoryTests
{
    [Fact]
    public void Create_ApiKey_UsesTheProtectedCredential()
    {
        var vault = new MemorySecretVault();
        vault.SetSecret("external-key", "test-api-key");

        IChatClient client = new OpenAiCompatibleChatClientFactory(vault).Create(CreateConnection(AuthenticationKind.ApiKey, "external-key"), CreateProfile());

        Assert.NotNull(client);
    }

    [Fact]
    public void Create_ApiKey_RejectsAMissingProtectedCredential()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            new OpenAiCompatibleChatClientFactory(new MemorySecretVault()).Create(CreateConnection(AuthenticationKind.ApiKey, "external-key"), CreateProfile()));

        Assert.Contains("protected credential", error.Message);
    }

    [Fact]
    public void Create_NoneAuthentication_CreatesAnOpenAiCompatibleChatClient()
    {
        IChatClient client = new OpenAiCompatibleChatClientFactory().Create(CreateConnection(AuthenticationKind.None, null), CreateProfile());

        Assert.NotNull(client);
    }

    private static ConnectionDefinition CreateConnection(AuthenticationKind authentication, string? reference) =>
        new("external", ConnectionCategory.External, "openai-compatible", "https://example.test/v1", true, authentication, reference);

    private static ModelProfile CreateProfile() =>
        new("external-default", "external", "compatible-model", 0.2f, TimeSpan.FromSeconds(30), new Dictionary<string, object?>());

    private sealed class MemorySecretVault : ISecretVault
    {
        private readonly Dictionary<string, string> secrets = new(StringComparer.Ordinal);

        public void SetSecret(string reference, string secret) => secrets[reference] = secret;
        public bool TryGetSecret(string reference, out string secret) => secrets.TryGetValue(reference, out secret!);
        public void RemoveSecret(string reference) => secrets.Remove(reference);
    }
}
