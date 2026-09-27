using TextAid.Core;

namespace TextAid.Core.Tests;

public sealed class ConnectionsAndProfilesTests
{
    [Fact]
    public void Resolve_UnconfiguredExternal_AutomaticallyDowngradesToLocalWithStatus()
    {
        ConfigurationSnapshot configuration = CreateConfiguration(
            externalEndpoint: string.Empty,
            externalModel: string.Empty,
            externalAuthentication: AuthenticationKind.ApiKey,
            externalSecretReference: "external-key");
        var resolver = new ProfileResolver(configuration, new MemorySecretVault());

        ProfileResolution result = resolver.Resolve("external-default");

        Assert.Equal(ProfileResolutionKind.AutomaticallyDowngraded, result.Kind);
        Assert.Equal("local-default", result.Profile!.Id);
        Assert.Equal(ConnectionCategory.ThisDeviceOnly, result.SelectedCategory);
        Assert.Contains("External configuration is not configured", result.Status);
        Assert.Contains("This device only", result.Status);
    }

    [Fact]
    public void Resolve_InvalidConfiguredExternal_OffersUserControlledDowngrade()
    {
        ConfigurationSnapshot configuration = CreateConfiguration(
            externalEndpoint: "not a valid endpoint",
            externalModel: "remote-model",
            externalAuthentication: AuthenticationKind.ApiKey,
            externalSecretReference: "external-key");
        var vault = new MemorySecretVault();
        vault.SetSecret("external-key", "secret-value");
        var resolver = new ProfileResolver(configuration, vault);

        ProfileResolution result = resolver.Resolve("external-default");

        Assert.Equal(ProfileResolutionKind.UserConfirmationRequired, result.Kind);
        Assert.Equal("local-default", result.Profile!.Id);
        Assert.Contains("invalid endpoint", result.Status);
        Assert.Contains("Continue with This device only", result.Status);
    }

    [Fact]
    public void OfferDowngradeAfterProviderFailure_RequiresUserConfirmation()
    {
        ConfigurationSnapshot configuration = CreateConfiguration(
            externalEndpoint: "https://example.test/v1",
            externalModel: "remote-model",
            externalAuthentication: AuthenticationKind.None,
            externalSecretReference: null);
        var resolver = new ProfileResolver(configuration, new MemorySecretVault());

        ProfileResolution result = resolver.OfferDowngradeAfterProviderFailure(ConnectionCategory.External, "The external provider did not respond.");

        Assert.Equal(ProfileResolutionKind.UserConfirmationRequired, result.Kind);
        Assert.Equal(ConnectionCategory.ThisDeviceOnly, result.SelectedCategory);
        Assert.Contains("did not respond", result.Status);
    }

    [Fact]
    public void Resolve_InactiveExternal_TreatsItAsUnconfiguredAndDowngradesAutomatically()
    {
        ConfigurationSnapshot configuration = CreateConfiguration(
            externalEndpoint: "https://example.test/v1",
            externalModel: "remote-model",
            externalAuthentication: AuthenticationKind.None,
            externalSecretReference: null,
            externalEnabled: false);
        var resolver = new ProfileResolver(configuration, new MemorySecretVault());

        ProfileResolution result = resolver.Resolve("external-default");

        Assert.Equal(ProfileResolutionKind.AutomaticallyDowngraded, result.Kind);
        Assert.Equal(ConnectionCategory.ThisDeviceOnly, result.SelectedCategory);
        Assert.Contains("inactive", result.Status);
    }

    [Fact]
    public void DpapiSecretVault_StoresEncryptedBytesOutsideConfiguration()
    {
        using var directory = new TemporaryDirectory();
        var vault = new DpapiSecretVault(directory.Path);

        vault.SetSecret("external-key", "not-clear-text");

        Assert.True(vault.TryGetSecret("external-key", out string secret));
        Assert.Equal("not-clear-text", secret);
        string file = Assert.Single(Directory.GetFiles(directory.Path, "*.secret"));
        Assert.DoesNotContain("not-clear-text", File.ReadAllText(file));
    }

    [Fact]
    public void ValidateConfiguration_RejectsDuplicateIdsInvalidLanguagesAndConflictingShortcuts()
    {
        ConfigurationSnapshot valid = CreateConfiguration("https://example.test/v1", "remote-model", AuthenticationKind.None, null);
        ConfigurationSnapshot duplicate = valid with { Connections = [valid.Connections[0], valid.Connections[0]] };
        ConfigurationSnapshot invalidLanguage = valid with { UserLanguage = "not-a-language" };
        ConfigurationSnapshot conflictingShortcuts = valid with { TranslationShortcut = valid.NormalShortcut };

        Assert.Throws<InvalidOperationException>(() => UserConfiguration.ValidateConfiguration(duplicate));
        Assert.Throws<InvalidOperationException>(() => UserConfiguration.ValidateConfiguration(invalidLanguage));
        Assert.Throws<InvalidOperationException>(() => UserConfiguration.ValidateConfiguration(conflictingShortcuts));
    }

    [Fact]
    public void ValidateConfiguration_AcceptsDistinctLanguageAndShortcutPreferences()
    {
        ConfigurationSnapshot configuration = CreateConfiguration("https://example.test/v1", "remote-model", AuthenticationKind.None, null) with
        {
            UserLanguage = "en-US",
            PreferredTranslationLanguage = "fr-FR",
            NormalShortcut = "Ctrl+Shift+R",
            TranslationShortcut = "Ctrl+Shift+T"
        };

        UserConfiguration.ValidateConfiguration(configuration);
    }

    private static ConfigurationSnapshot CreateConfiguration(string externalEndpoint, string externalModel, AuthenticationKind externalAuthentication, string? externalSecretReference, bool externalEnabled = true)
    {
        var connections = new[]
        {
            new ConnectionDefinition("ollama-local", ConnectionCategory.ThisDeviceOnly, "ollama", "http://127.0.0.1:11434", true, AuthenticationKind.None, null),
            new ConnectionDefinition("openai-external", ConnectionCategory.External, "openai-compatible", externalEndpoint, externalEnabled, externalAuthentication, externalSecretReference)
        };
        var profiles = new[]
        {
            new ModelProfile("local-default", "ollama-local", "local-model", 0.2f, TimeSpan.FromSeconds(30), new Dictionary<string, object?>()),
            new ModelProfile("external-default", "openai-external", externalModel, 0.2f, TimeSpan.FromSeconds(30), new Dictionary<string, object?>())
        };
        return new ConfigurationSnapshot(1, connections, profiles, "en", "fr", "Ctrl+C+C", "Ctrl+C+T");
    }

    private sealed class MemorySecretVault : ISecretVault
    {
        private readonly Dictionary<string, string> secrets = new(StringComparer.Ordinal);

        public void SetSecret(string reference, string secret) => secrets[reference] = secret;
        public bool TryGetSecret(string reference, out string secret) => secrets.TryGetValue(reference, out secret!);
        public void RemoveSecret(string reference) => secrets.Remove(reference);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TextAid.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }
}
