using TextAid.Core;

namespace TextAid.Core.Tests;

public sealed class ConnectionsAndProfilesTests
{
    [Theory]
    [InlineData("localhost", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("192.168.1.20", false)]
    [InlineData("example.test", false)]
    public void LoopbackClassification_AllowsOnlyThisDeviceHosts(string host, bool expected)
    {
        Assert.Equal(expected, UserConfiguration.IsLoopbackHost(host));
    }

    [Fact]
    public void DebugLog_Disabled_DoesNotCreateAFile()
    {
        using var directory = new TemporaryDirectory();

        using var log = DebugSessionLog.Start(false, false, directory.Path);
        log.Write("ignored", ("inputLength", 42));

        Assert.Null(log.Path);
        Assert.Empty(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public void DebugLog_Enabled_TruncatesAndNeverWritesSensitiveValues()
    {
        using var directory = new TemporaryDirectory();
        string secret = "api-key-sentinel-123";
        string userText = "clipboard-sentinel-456";
        string path;
        using (var first = DebugSessionLog.Start(true, false, directory.Path))
        {
            first.Write("first-event", ("inputLength", userText.Length), ("secret", secret), ("input", userText));
            first.WriteException("provider", new InvalidOperationException($"Failed for {secret} and {userText}"));
            path = first.Path!;
        }

        using (var second = DebugSessionLog.Start(true, false, directory.Path))
        {
            second.Write("second-event", ("outputLength", 7));
            second.Write("user-facing-failure", ("category", UserFacingFailure.Configuration));
            try { throw new InvalidOperationException($"Failed for {secret} and {userText}"); }
            catch (InvalidOperationException exception) { second.WriteException("provider", exception); }
        }

        string content = File.ReadAllText(path);
        Assert.Contains("event=second-event", content);
        Assert.Contains("event=user-facing-failure category=Configuration", content);
        Assert.Contains("event=exception operation=provider", content);
        Assert.Contains("event=exception-stack operation=provider", content);
        Assert.DoesNotContain("first-event", content);
        Assert.DoesNotContain(secret, content);
        Assert.DoesNotContain(userText, content);
    }

    [Fact]
    public void DebugLog_FullMode_WritesDiagnosticTextButRedactsCredentials()
    {
        using var directory = new TemporaryDirectory();
        const string input = "The transformation failed for this sentence.";
        const string apiKey = "sk-secret-api-key-123";
        const string bearerToken = "Bearer token-secret-456";

        using (var log = DebugSessionLog.Start(true, true, directory.Path))
        {
            log.WriteFullText("transformation-input", $"{input} api_key={apiKey} {bearerToken}");
            try { throw new InvalidOperationException($"Provider returned {apiKey}"); }
            catch (InvalidOperationException exception) { log.WriteException("transformation", exception); }
        }

        string content = File.ReadAllText(Path.Combine(directory.Path, "TextAid.debug.log"));
        Assert.Contains(input, content);
        Assert.Contains("event=exception-message operation=transformation", content);
        Assert.DoesNotContain(apiKey, content);
        Assert.DoesNotContain("token-secret-456", content);
    }

    [Theory]
    [InlineData(UserFacingFailure.Clipboard)]
    [InlineData(UserFacingFailure.Configuration)]
    [InlineData(UserFacingFailure.Provider)]
    [InlineData(UserFacingFailure.Model)]
    [InlineData(UserFacingFailure.Cancellation)]
    [InlineData(UserFacingFailure.SourceWindow)]
    [InlineData(UserFacingFailure.Paste)]
    [InlineData(UserFacingFailure.Localization)]
    public void UserFacingErrors_ProvideRecoveryGuidance(UserFacingFailure failure)
    {
        string message = UserFacingErrorMapper.GetMessage(failure);

        Assert.NotEmpty(message);
        Assert.DoesNotContain("Win32", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COM", message, StringComparison.OrdinalIgnoreCase);
    }

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
    public void Resolve_OnlyActiveExternal_PromotesLocalActionAutomaticallyAndVisibly()
    {
        ConfigurationSnapshot configuration = CreateConfiguration("https://example.test/v1", "remote-model", AuthenticationKind.None, null, localEnabled: false);
        var resolver = new ProfileResolver(configuration, new MemorySecretVault());

        ProfileResolution result = resolver.Resolve("local-default");

        Assert.Equal(ProfileResolutionKind.AutomaticallyPromoted, result.Kind);
        Assert.Equal("external-default", result.Profile!.Id);
        Assert.Equal(ConnectionCategory.External, result.SelectedCategory);
        Assert.Contains("requested This device only", result.Status);
        Assert.Contains("only active External", result.Status);
    }

    [Fact]
    public void Resolve_MultipleEligibleRemoteChoices_DoesNotPromoteLocalActionAutomatically()
    {
        var connections = new[]
        {
            new ConnectionDefinition("ollama-local", ConnectionCategory.ThisDeviceOnly, "ollama", "http://127.0.0.1:11434", false, AuthenticationKind.None, null),
            new ConnectionDefinition("ollama-network", ConnectionCategory.OnPremises, "ollama", "http://network.test:11434", true, AuthenticationKind.None, null),
            new ConnectionDefinition("openai-external", ConnectionCategory.External, "openai-compatible", "https://example.test/v1", true, AuthenticationKind.None, null)
        };
        var profiles = new[]
        {
            new ModelProfile("local-default", "ollama-local", "local-model", 0.2f, TimeSpan.FromSeconds(30), new Dictionary<string, object?>()),
            new ModelProfile("network-default", "ollama-network", "network-model", 0.2f, TimeSpan.FromSeconds(30), new Dictionary<string, object?>()),
            new ModelProfile("external-default", "openai-external", "remote-model", 0.2f, TimeSpan.FromSeconds(30), new Dictionary<string, object?>())
        };
        var resolver = new ProfileResolver(new ConfigurationSnapshot(1, connections, profiles, "en", "fr", "Ctrl+C+C", "Ctrl+C+T"), new MemorySecretVault());

        ProfileResolution result = resolver.Resolve("local-default");

        Assert.Equal(ProfileResolutionKind.Failed, result.Kind);
        Assert.Contains("Multiple active configurations", result.Status);
        Assert.Contains("appropriate profile", result.Status);
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

    private static ConfigurationSnapshot CreateConfiguration(string externalEndpoint, string externalModel, AuthenticationKind externalAuthentication, string? externalSecretReference, bool externalEnabled = true, bool localEnabled = true)
    {
        var connections = new[]
        {
            new ConnectionDefinition("ollama-local", ConnectionCategory.ThisDeviceOnly, "ollama", "http://127.0.0.1:11434", localEnabled, AuthenticationKind.None, null),
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
