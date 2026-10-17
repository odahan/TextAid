using TextAid.Core;

namespace TextAid.Core.Tests;

public sealed class ModernizationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(123)]
    public async Task Transfer_PreservesRawTextTargetAndLanguageAcrossExplicitGenerations(int target)
    {
        using var session = new InvocationSession(target, "original");
        session.SelectAutomaticTranslationDestination("fr", "en", "fr");
        int generation = session.StartNewGeneration();
        const string markdown = "  # Title\n\n**raw**\n";
        Assert.True(session.TryCompleteGeneration(generation, markdown, "en"));
        Assert.True(session.TransferResultToInput());
        Assert.Equal(markdown, session.InputText);
        Assert.Equal(target, session.SourceWindow);
        Assert.Null(session.OutputText);
        Assert.False(session.HasCurrentResult);
        Assert.Equal("Unchanged", session.OutputLanguage);
        int calls = 0;
        Assert.Equal("en", await session.DetectInputLanguageAsync((_, _) => { calls++; return Task.FromResult<string?>("de"); }));
        Assert.Equal(0, calls);
        generation = session.StartNewGeneration();
        Assert.True(session.TryCompleteGeneration(generation, "second", "fr"));
        session.OutputLanguage = "de";
        Assert.True(session.TransferResultToInput());
        Assert.Equal("second", session.InputText);
        Assert.Equal("fr", session.DetectedInputLanguage);
        Assert.Equal("de", session.OutputLanguage);
        session.SetInputText("edited");
        Assert.Null(session.DetectedInputLanguage);
        await session.DetectInputLanguageAsync((_, _) => { calls++; return Task.FromResult<string?>("de"); });
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Transfer_RejectsLateLanguageAndGenerationUpdatesEvenWhenTextIsUnchanged()
    {
        using var session = new InvocationSession(123, "same");
        var pending = new TaskCompletionSource<string?>();
        Task<string?> detection = session.DetectInputLanguageAsync((_, _) => pending.Task);
        int generation = session.StartNewGeneration();
        Assert.False(session.TransferResultToInput());
        Assert.True(session.TryCompleteGeneration(generation, "same", "en"));
        Assert.True(session.TransferResultToInput());
        pending.SetResult("fr");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => detection);
        Assert.Equal("en", session.DetectedInputLanguage);
        Assert.False(session.TryCompleteGeneration(generation, "obsolete", "fr"));
        Assert.False(session.HasCurrentResult);
    }

    [Theory]
    [InlineData("external-default", "account-profile")]
    [InlineData("account-profile", "account-profile")]
    [InlineData("local-default", "local-default")]
    [InlineData("network-default", "network-default")]
    public void DefaultRouting_PrefersAccountOnlyWithinExternal(string requested, string expected)
    {
        var resolver = new ProfileResolver(Configuration(), Vault());
        ProfileResolution result = resolver.Resolve(requested);
        Assert.Equal(expected, result.Profile?.Id);
        Assert.Equal(ProfileResolutionKind.Selected, result.Kind);
    }

    [Theory]
    [InlineData("external-default")]
    [InlineData("local-default")]
    [InlineData("network-default")]
    public void ExactOverride_IsAuthoritative(string requested)
    {
        var resolver = new ProfileResolver(Configuration(), Vault());
        Assert.Equal(requested, resolver.Resolve(requested, exact: true).Profile?.Id);
    }

    [Fact]
    public void BothCloudOnly_ResolvesSoleCategoryToAccount()
    {
        ConfigurationSnapshot config = Configuration();
        config = config with { Connections = config.Connections.Select(connection => connection with { IsEnabled = connection.Category == ConnectionCategory.External }).ToArray() };
        ProfileResolution result = new ProfileResolver(config, Vault()).Resolve("local-default");
        Assert.Equal("account-profile", result.Profile?.Id);
        Assert.Equal(ProfileResolutionKind.AutomaticallyPromoted, result.Kind);
    }

    [Fact]
    public void InvalidConfiguredAccount_NeverSilentlyUsesKeyPeer()
    {
        var resolver = new ProfileResolver(Configuration(), new MemoryVault());
        ProfileResolution result = resolver.Resolve("external-default");
        Assert.NotEqual("external-default", result.Profile?.Id);
        Assert.Equal(ProfileResolutionKind.UserConfirmationRequired, result.Kind);
        Assert.Equal(ProfileResolutionKind.Failed, resolver.Resolve("account-profile", exact: true).Kind);
    }

    [Fact]
    public void InactiveAccount_IsIgnored()
    {
        ConfigurationSnapshot config = Configuration();
        config = config with { Connections = config.Connections.Select(connection => connection.Provider == "openai-account" ? connection with { IsEnabled = false } : connection).ToArray() };
        Assert.Equal("external-default", new ProfileResolver(config, Vault()).Resolve("external-default").Profile?.Id);
        Assert.Equal("external-default", new ProfileResolver(config, Vault()).Resolve("account-profile").Profile?.Id);
    }

    [Fact]
    public void Migration_AddsInactiveAccountWithoutChangingExistingKeysOrProfiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TextAid-014-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "config.json");
        try
        {
            const string original = "{\"schemaVersion\":1,\"connections\":[{\"id\":\"ollama-local\",\"category\":\"ThisDeviceOnly\",\"isEnabled\":true},{\"id\":\"openai-external\",\"category\":\"External\",\"isEnabled\":true,\"secretReference\":\"existing-key\"}],\"profiles\":[{\"id\":\"external-default\",\"connectionId\":\"openai-external\",\"model\":\"saved-model\"}]}";
            File.WriteAllText(path, original);
            var upgrade = typeof(UserConfiguration).GetMethod("UpgradeVersionOneConfiguration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            upgrade.Invoke(null, [path]);
            var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
            var connections = root["connections"]!.AsArray();
            var account = connections.Single(connection => (string?)connection!["id"] == "openai-account")!;
            Assert.False((bool)account["isEnabled"]!);
            Assert.Equal("OpenAiAccount", (string?)account["authentication"]);
            Assert.Equal("existing-key", (string?)connections.Single(connection => (string?)connection!["id"] == "openai-external")!["secretReference"]);
            Assert.Equal("saved-model", (string?)root["profiles"]!.AsArray().Single(profile => (string?)profile!["id"] == "external-default")!["model"]);
            string upgraded = File.ReadAllText(path);
            upgrade.Invoke(null, [path]);
            Assert.Equal(upgraded, File.ReadAllText(path));
        }
        finally { File.Delete(path); Directory.Delete(directory); }
    }

    private static ConfigurationSnapshot Configuration() => new(1,
    [
        new("local", ConnectionCategory.ThisDeviceOnly, "ollama", "http://127.0.0.1:11434", true, AuthenticationKind.None, null),
        new("network", ConnectionCategory.OnPremises, "ollama", "http://server:11434", true, AuthenticationKind.None, null),
        new("key", ConnectionCategory.External, "openai-compatible", "https://provider.example/v1", true, AuthenticationKind.None, null),
        new("account", ConnectionCategory.External, "openai-account", UserConfiguration.DefaultExternalEndpoint, true, AuthenticationKind.OpenAiAccount, "session")
    ],
    [
        Profile("local-default", "local"), Profile("network-default", "network"), Profile("external-default", "key"), Profile("account-profile", "account")
    ], "en", "fr", "Ctrl+C+C", "Ctrl+C+T");

    private static ModelProfile Profile(string id, string connection) => new(id, connection, "model", 0.2f, TimeSpan.FromMinutes(2), new Dictionary<string, object?>());
    private static MemoryVault Vault() => new() { Value = "{\"access_token\":\"synthetic\",\"scope\":\"chatgpt.tokens.use.direct\"}" };
    private sealed class MemoryVault : ISecretVault
    {
        public string? Value { get; set; }
        public void SetSecret(string reference, string secret) => Value = secret;
        public bool TryGetSecret(string reference, out string secret) { secret = Value ?? ""; return Value is not null; }
        public void RemoveSecret(string reference) => Value = null;
    }
}
