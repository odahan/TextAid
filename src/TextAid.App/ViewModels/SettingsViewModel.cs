using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextAid.AI;
using TextAid.Core;
using TextAid.App.Localization;
using TextAid.Platform.Windows;

namespace TextAid.App.ViewModels;

/// <summary>Represents the outcome of an explicit Ollama connection check.</summary>
public enum ConnectionTestState
{
    NotTested,
    Testing,
    Ready,
    Failed
}

/// <summary>Identifies the locale selected when a complete UI translation was generated.</summary>
public sealed class UiTranslationGeneratedEventArgs(string language, bool preferEnglishUi) : EventArgs
{
    /// <summary>Gets the language code used to generate the translation.</summary>
    public string Language { get; } = language;

    /// <summary>Gets whether the English interface was selected during generation.</summary>
    public bool PreferEnglishUi { get; } = preferEnglishUi;
}

/// <summary>Edits local, On-premises, and External connection settings.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly OllamaChatClientFactory chatClientFactory = new();
    private readonly OpenAiCompatibleChatClientFactory externalChatClientFactory = new();
    private readonly AiClientFactory aiClientFactory = new();
    private readonly IWindowsStartupRegistration startupRegistration = new WindowsStartupRegistration();

    [ObservableProperty] private string endpoint = "http://127.0.0.1:11434";
    [ObservableProperty] private string? selectedModel;
    [ObservableProperty] private string temperatureText = "0.2";
    [ObservableProperty] private string contextSizeText = "8192";
    [ObservableProperty] private string timeoutSecondsText = "120";
    [ObservableProperty] private ThinkingMode selectedThinking = ThinkingMode.Off;
    [ObservableProperty] private string status = UiStrings.Get("LocalModelsHelp");
    [ObservableProperty] private bool isLoadingModels;
    [ObservableProperty] private bool isLoadingNetworkModels;
    [ObservableProperty] private ConnectionTestState connectionState = ConnectionTestState.NotTested;
    [ObservableProperty] private ConnectionTestState networkConnectionState = ConnectionTestState.NotTested;
    [ObservableProperty] private ConnectionTestState externalConnectionState = ConnectionTestState.NotTested;
    [ObservableProperty] private string networkEndpoint = string.Empty;
    [ObservableProperty] private string networkModel = string.Empty;
    [ObservableProperty] private string networkSecret = string.Empty;
    [ObservableProperty] private string externalEndpoint = UserConfiguration.DefaultExternalEndpoint;
    [ObservableProperty] private string externalModel = string.Empty;
    [ObservableProperty] private string externalSecret = string.Empty;
    [ObservableProperty] private bool localEnabled = true;
    [ObservableProperty] private bool networkEnabled;
    [ObservableProperty] private bool externalEnabled;
    [ObservableProperty] private bool debugEnabled;
    [ObservableProperty] private bool fullDebugEnabled;
    [ObservableProperty] private string userLanguage = "en";
    [ObservableProperty] private string preferredTranslationLanguage = "fr";
    [ObservableProperty] private string normalShortcut = "Ctrl+C+C";
    [ObservableProperty] private string translationShortcut = "Ctrl+C+T";
    [ObservableProperty] private bool preferEnglishUi;
    [ObservableProperty] private string selectedLocalizationProfile = "local-default";
    [ObservableProperty] private bool isGeneratingLocale;
    [ObservableProperty] private bool isLoadingExternalModels;
    [ObservableProperty] private bool startWithWindows;
    [ObservableProperty] private bool openAiAccountEnabled;
    [ObservableProperty] private string openAiAccountModel = string.Empty;
    [ObservableProperty] private string openAiAccountStatus = UiStrings.Get("OpenAiDisconnected");
    [ObservableProperty] private bool isOpenAiBusy;
    private readonly OpenAiAccountSession accountSession = OpenAiAccountSession.Create();
    private CancellationTokenSource? accountCancellation;
    public ObservableCollection<OpenAiAccountModel> OpenAiAccountModels { get; } = [];

    /// <summary>Cancels account work when Settings closes or the user cancels sign-in.</summary>
    [RelayCommand]
    public void CancelOpenAiSignIn() => accountCancellation?.Cancel();

    /// <summary>Starts TextAid's own cancellable system-browser registration.</summary>
    [RelayCommand]
    private async Task SignInOpenAiAsync()
    {
        if (IsOpenAiBusy) return;
        IsOpenAiBusy = true;
        accountCancellation = new CancellationTokenSource();
        try
        {
            await accountSession.SignInAsync(new Progress<string>(key => OpenAiAccountStatus = UiStrings.Get(key)), accountCancellation.Token);
            OpenAiAccountStatus = UiStrings.Get(accountSession.StatusKey);
        }
        catch (OperationCanceledException) { OpenAiAccountStatus = UiStrings.Get("OpenAiSignInCancelled"); }
        catch (OpenAiAccountException error) { OpenAiAccountStatus = UiStrings.TryGet(error.Message) ?? UiStrings.Get("OpenAiRequestFailed"); }
        catch { OpenAiAccountStatus = UiStrings.Get("OpenAiRequestFailed"); }
        finally { IsOpenAiBusy = false; accountCancellation.Dispose(); accountCancellation = null; }
    }

    /// <summary>Loads offered models without a test generation.</summary>
    [RelayCommand]
    private async Task LoadOpenAiModelsAsync()
    {
        if (IsOpenAiBusy) return;
        IsOpenAiBusy = true;
        accountCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            IReadOnlyList<OpenAiAccountModel> models = await accountSession.GetModelsAsync(accountCancellation.Token);
            string selected = OpenAiAccountModel;
            OpenAiAccountModels.Clear();
            foreach (var model in models) OpenAiAccountModels.Add(model);
            if (!string.IsNullOrWhiteSpace(selected) && !models.Any(model => model.Slug == selected))
                OpenAiAccountModels.Add(new(selected, selected));
            OpenAiAccountModel = selected;
            OpenAiAccountStatus = UiStrings.Get(models.Count == 0 ? "OpenAiNoModels" : "OpenAiChooseModel");
        }
        catch { OpenAiAccountStatus = UiStrings.Get("OpenAiModelsFailed"); }
        finally { IsOpenAiBusy = false; accountCancellation.Dispose(); accountCancellation = null; }
    }

    /// <summary>Tests the specifically selected account profile through the production MAF boundary.</summary>
    [RelayCommand]
    private async Task TestOpenAiAsync()
    {
        if (IsOpenAiBusy) return;
        IsOpenAiBusy = true;
        accountCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            if (string.IsNullOrWhiteSpace(OpenAiAccountModel) || !(await accountSession.GetModelsAsync(accountCancellation.Token)).Any(model => model.Slug == OpenAiAccountModel))
                throw new OpenAiAccountException("OpenAiChooseModel");
            var connection = new ConnectionDefinition("openai-account", ConnectionCategory.External, "openai-account", UserConfiguration.DefaultExternalEndpoint, true, AuthenticationKind.OpenAiAccount, "openai-account-session");
            var profile = new ModelProfile("openai-account-default", connection.Id, OpenAiAccountModel, 0.2f, TimeSpan.FromMinutes(2), new Dictionary<string, object?>());
            var service = new MafTextTransformationService(_ => new AiClientFactory().Create(connection, profile), false, honorConfiguredTimeout: true);
            await service.TransformAsync(new TextTransformationRequest("Hello.", "Return a short greeting.", new TextTransformationSettings(connection.Endpoint, profile.Model, profile.Temperature, profile.Timeout, 8192, ThinkingMode.Off)), accountCancellation.Token);
            OpenAiAccountStatus = UiStrings.Get("OpenAiTestSucceeded");
        }
        catch (OpenAiAccountException error) { OpenAiAccountStatus = UiStrings.TryGet(error.Message) ?? UiStrings.Get("OpenAiRequestFailed"); }
        catch { OpenAiAccountStatus = UiStrings.Get("OpenAiRequestFailed"); }
        finally { IsOpenAiBusy = false; accountCancellation.Dispose(); accountCancellation = null; }
    }

    /// <summary>Revokes the renewable session where possible and always signs out locally.</summary>
    [RelayCommand]
    private async Task SignOutOpenAiAsync()
    {
        if (IsOpenAiBusy) return;
        IsOpenAiBusy = true;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            bool revoked = await accountSession.SignOutAsync(timeout.Token);
            OpenAiAccountStatus = UiStrings.Get(revoked ? "OpenAiSignedOut" : "OpenAiLocalSignOut");
        }
        catch { OpenAiAccountStatus = UiStrings.Get("OpenAiStorageFailed"); }
        finally { IsOpenAiBusy = false; }
    }

    public ObservableCollection<string> Models { get; } = [];
    public ObservableCollection<string> NetworkModels { get; } = [];
    public ObservableCollection<string> ExternalModels { get; } = [];
    public IReadOnlyList<LanguageOption> Languages => LanguageCatalog.Supported;
    public IReadOnlyList<ThinkingMode> ThinkingModes { get; } = Enum.GetValues<ThinkingMode>();
    public ObservableCollection<string> LocalizationProfiles { get; } = [];
    public ObservableCollection<ProfileSummary> ProfileSummaries { get; } = [];
    public event EventHandler? Saved;
    /// <summary>Raised after a complete, valid UI translation catalog is saved locally.</summary>
    public event EventHandler<UiTranslationGeneratedEventArgs>? UiTranslationGenerated;
    public bool IsFullLogActive => DebugEnabled && FullDebugEnabled;

    partial void OnDebugEnabledChanged(bool value) => OnPropertyChanged(nameof(IsFullLogActive));
    partial void OnFullDebugEnabledChanged(bool value) => OnPropertyChanged(nameof(IsFullLogActive));

    /// <summary>Loads persisted settings and attempts local model discovery.</summary>
    public async Task InitializeAsync()
    {
        try
        {
            TextTransformationSettings settings = UserConfiguration.LoadTransformationSettings();
            Endpoint = settings.Endpoint;
            SelectedModel = settings.Model;
            TemperatureText = settings.Temperature.ToString(CultureInfo.InvariantCulture);
            ContextSizeText = settings.ContextSize.ToString(CultureInfo.InvariantCulture);
            TimeoutSecondsText = ((int)settings.Timeout.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            SelectedThinking = settings.Thinking;
            ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
            UserLanguage = configuration.UserLanguage;
            PreferredTranslationLanguage = configuration.PreferredTranslationLanguage;
            NormalShortcut = configuration.NormalShortcut;
            TranslationShortcut = configuration.TranslationShortcut;
            LocalizationPreferences localization = UserConfiguration.LoadLocalizationPreferences();
            PreferEnglishUi = localization.PreferEnglishUi;
            LocalizationProfiles.Clear();
            ProfileSummaries.Clear();
            foreach (ModelProfile profile in configuration.Profiles) LocalizationProfiles.Add(profile.Id);
            foreach (ModelProfile profile in configuration.Profiles)
            {
                ConnectionDefinition connection = configuration.Connections.Single(candidate => candidate.Id.Equals(profile.ConnectionId, StringComparison.OrdinalIgnoreCase));
                ProfileSummaries.Add(new ProfileSummary(profile.Id, connection.Category.ToString(), string.IsNullOrWhiteSpace(profile.Model) ? UiStrings.Get("NoModelConfiguredLabel") : profile.Model, connection.IsEnabled));
            }
            SelectedLocalizationProfile = LocalizationProfiles.Contains(localization.ProfileId) ? localization.ProfileId : LocalizationProfiles.FirstOrDefault() ?? "local-default";
            ConnectionDefinition local = configuration.Connections.Single(connection => connection.Category == ConnectionCategory.ThisDeviceOnly);
            LocalEnabled = local.IsEnabled;
            ConnectionDefinition? network = configuration.Connections.FirstOrDefault(connection => connection.Category == ConnectionCategory.OnPremises);
            ModelProfile? networkProfile = network is null ? null : configuration.Profiles.FirstOrDefault(profile => profile.ConnectionId.Equals(network.Id, StringComparison.OrdinalIgnoreCase));
            NetworkEndpoint = network?.Endpoint ?? string.Empty;
            NetworkModel = networkProfile?.Model ?? string.Empty;
            PopulateNetworkModels(NetworkModel, []);
            NetworkEnabled = network?.IsEnabled ?? false;
            ConnectionDefinition? external = configuration.Connections.FirstOrDefault(connection => connection.Provider == "openai-compatible");
            ConnectionDefinition? account = configuration.Connections.FirstOrDefault(connection => connection.Provider == "openai-account");
            OpenAiAccountEnabled = account?.IsEnabled ?? false;
            OpenAiAccountModel = configuration.Profiles.FirstOrDefault(profile => profile.ConnectionId == account?.Id)?.Model ?? string.Empty;
            OpenAiAccountModels.Clear();
            if (!string.IsNullOrWhiteSpace(OpenAiAccountModel)) OpenAiAccountModels.Add(new(OpenAiAccountModel, OpenAiAccountModel));
            OpenAiAccountStatus = UiStrings.Get(accountSession.StatusKey);
            ModelProfile? externalProfile = external is null ? null : configuration.Profiles.FirstOrDefault(profile => profile.ConnectionId.Equals(external.Id, StringComparison.OrdinalIgnoreCase));
            ExternalEndpoint = string.IsNullOrWhiteSpace(external?.Endpoint) ? UserConfiguration.DefaultExternalEndpoint : external.Endpoint;
            ExternalModel = externalProfile?.Model ?? string.Empty;
            PopulateExternalModels(ExternalModel, []);
            ExternalEnabled = external?.IsEnabled ?? false;
            DebugEnabled = UserConfiguration.LoadDebugMode();
            FullDebugEnabled = UserConfiguration.LoadFullDebugMode();
            StartWithWindows = startupRegistration.IsEnabled();
            await LoadModelsAsync();
        }
        catch (Exception)
        {
            Status = UserFacingErrorMapper.GetMessage(UserFacingFailure.Configuration);
        }
    }

    [RelayCommand]
    private async Task LoadModelsAsync()
    {
        IsLoadingModels = true;
        try
        {
            IReadOnlyList<string> models = await chatClientFactory.GetLocalModelNamesAsync(Endpoint.Trim(), CancellationToken.None);
            Models.Clear();
            foreach (string model in models) Models.Add(model);
            Status = models.Count == 0 ? UiStrings.Get("NoLocalModelsMessage") : UiStrings.Get("ChooseLocalModelMessage");
        }
        catch
        {
            Models.Clear();
            Status = UiStrings.Get("OllamaUnreachableMessage");
        }
        finally { IsLoadingModels = false; }
    }

    /// <summary>Loads models exposed by the configured On-premises Ollama server.</summary>
    [RelayCommand]
    private async Task LoadNetworkModelsAsync()
    {
        IsLoadingNetworkModels = true;
        try
        {
            if (string.IsNullOrWhiteSpace(NetworkEndpoint))
                throw new InvalidOperationException("Enter the Ollama server address first.");

            IReadOnlyList<string> models = await chatClientFactory.GetLocalModelNamesAsync(NetworkEndpoint.Trim(), CancellationToken.None);
            PopulateNetworkModels(NetworkModel, models);
            NetworkConnectionState = ConnectionTestState.Ready;
            Status = models.Count == 0 ? UiStrings.Get("NoNetworkModelsMessage") : UiStrings.Get("ChooseNetworkModelMessage");
        }
        catch
        {
            NetworkModels.Clear();
            NetworkConnectionState = ConnectionTestState.Failed;
            Status = UiStrings.Get("NetworkModelsUnavailableMessage");
        }
        finally { IsLoadingNetworkModels = false; }
    }

    /// <summary>Tests the configured On-premises Ollama server without saving settings.</summary>
    [RelayCommand]
    private async Task TestNetworkConnectionAsync()
    {
        NetworkConnectionState = ConnectionTestState.Testing;
        Status = UiStrings.Get("TestingNetworkOllamaMessage");
        try
        {
            bool isRunning = await chatClientFactory.TestConnectionAsync(NetworkEndpoint.Trim(), CancellationToken.None);
            NetworkConnectionState = isRunning ? ConnectionTestState.Ready : ConnectionTestState.Failed;
            Status = isRunning ? UiStrings.Get("NetworkOllamaAvailableMessage") : UiStrings.Get("NetworkOllamaRejectedMessage");
        }
        catch
        {
            NetworkConnectionState = ConnectionTestState.Failed;
            Status = UiStrings.Get("NetworkOllamaUnreachableMessage");
        }
    }

    /// <summary>Loads models exposed by the configured OpenAI-compatible endpoint.</summary>
    [RelayCommand]
    private async Task LoadExternalModelsAsync()
    {
        IsLoadingExternalModels = true;
        try
        {
            var connection = new ConnectionDefinition("openai-external", ConnectionCategory.External, "openai-compatible", ExternalEndpoint.Trim(), true, AuthenticationKind.ApiKey, "external-api-key");
            IReadOnlyList<string> models = await externalChatClientFactory.GetModelNamesAsync(connection, ExternalSecret, CancellationToken.None);
            PopulateExternalModels(ExternalModel, models);
            Status = models.Count == 0 ? UiStrings.Get("NoExternalModelsMessage") : UiStrings.Get("ChooseExternalModelMessage");
        }
        catch
        {
            ExternalModels.Clear();
            Status = UiStrings.Get("ExternalModelsUnavailableMessage");
        }
        finally { IsLoadingExternalModels = false; }
    }

    /// <summary>Tests the configured OpenAI-compatible endpoint with the entered or saved API key.</summary>
    [RelayCommand]
    private async Task TestExternalConnectionAsync()
    {
        ExternalConnectionState = ConnectionTestState.Testing;
        Status = UiStrings.Get("TestingExternalConnectionMessage");
        try
        {
            var connection = new ConnectionDefinition("openai-external", ConnectionCategory.External, "openai-compatible", ExternalEndpoint.Trim(), true, AuthenticationKind.ApiKey, "external-api-key");
            _ = await externalChatClientFactory.GetModelNamesAsync(connection, ExternalSecret, CancellationToken.None);
            ExternalConnectionState = ConnectionTestState.Ready;
            Status = UiStrings.Get("ExternalConnectionAvailableMessage");
        }
        catch
        {
            ExternalConnectionState = ConnectionTestState.Failed;
            Status = UiStrings.Get("ExternalConnectionUnavailableMessage");
        }
    }

    /// <summary>Keeps a persisted External model visible while merging newly discovered model names.</summary>
    private void PopulateExternalModels(string selectedModel, IEnumerable<string> discoveredModels)
    {
        ExternalModels.Clear();
        if (!string.IsNullOrWhiteSpace(selectedModel)) ExternalModels.Add(selectedModel);
        foreach (string model in discoveredModels)
        {
            if (!ExternalModels.Contains(model, StringComparer.OrdinalIgnoreCase)) ExternalModels.Add(model);
        }
        ExternalModel = selectedModel;
        OnPropertyChanged(nameof(ExternalModel));
    }

    /// <summary>Keeps a persisted On-premises model visible while merging discovered Ollama model names.</summary>
    private void PopulateNetworkModels(string selectedModel, IEnumerable<string> discoveredModels)
    {
        NetworkModels.Clear();
        if (!string.IsNullOrWhiteSpace(selectedModel)) NetworkModels.Add(selectedModel);
        foreach (string model in discoveredModels)
        {
            if (!NetworkModels.Contains(model, StringComparer.OrdinalIgnoreCase)) NetworkModels.Add(model);
        }
        NetworkModel = selectedModel;
        OnPropertyChanged(nameof(NetworkModel));
    }

    /// <summary>Reloads persisted configuration only after the existing state can remain safe on a failure.</summary>
    [RelayCommand]
    private async Task ReloadConfigurationAsync()
    {
        try
        {
            _ = UserConfiguration.ReloadConfiguration();
            await InitializeAsync();
            Status = UiStrings.Get("ConfigurationReloadedMessage");
        }
        catch
        {
            Status = UiStrings.Get("ConfigurationReloadFailureMessage");
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        ConnectionState = ConnectionTestState.Testing;
        Status = UiStrings.Get("TestingOllamaMessage");
        try
        {
            bool isRunning = await chatClientFactory.TestConnectionAsync(Endpoint.Trim(), CancellationToken.None);
            ConnectionState = isRunning ? ConnectionTestState.Ready : ConnectionTestState.Failed;
            Status = isRunning ? UiStrings.Get("OllamaAvailableMessage") : UiStrings.Get("OllamaConnectionRejectedMessage");
        }
        catch
        {
            ConnectionState = ConnectionTestState.Failed;
            Status = UiStrings.Get("OllamaEndpointUnreachableMessage");
        }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            TextTransformationSettings? localSettings = null;
            if (LocalEnabled)
            {
                if (!float.TryParse(TemperatureText, NumberStyles.Float, CultureInfo.InvariantCulture, out float temperature)) throw new ArgumentException("Temperature must be a number between 0 and 2.");
                if (!int.TryParse(ContextSizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int contextSize)) throw new ArgumentException("Context size must be a whole number.");
                if (!int.TryParse(TimeoutSecondsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int timeoutSeconds)) throw new ArgumentException("Timeout must be a whole number of seconds.");
                localSettings = new TextTransformationSettings(Endpoint.Trim(), SelectedModel ?? string.Empty, temperature, TimeSpan.FromSeconds(timeoutSeconds), contextSize, SelectedThinking);
            }
            UserConfiguration.SaveSettings(new UserConfiguration.SettingsSaveRequest(
                LocalEnabled,
                localSettings,
                NetworkEnabled,
                NetworkEndpoint,
                NetworkModel,
                NetworkSecret,
                ExternalEnabled,
                ExternalEndpoint,
                ExternalModel,
                ExternalSecret,
                DebugEnabled,
                FullDebugEnabled,
                UserLanguage,
                PreferredTranslationLanguage,
                NormalShortcut,
                TranslationShortcut,
                PreferEnglishUi,
                SelectedLocalizationProfile,
                OpenAiAccountEnabled,
                OpenAiAccountModel));
            try
            {
                startupRegistration.SetEnabled(StartWithWindows);
            }
            catch
            {
                try { StartWithWindows = startupRegistration.IsEnabled(); }
                catch { }
                NetworkSecret = string.Empty;
                ExternalSecret = string.Empty;
                Saved?.Invoke(this, EventArgs.Empty);
                Status = UiStrings.Get("WindowsStartupUpdateFailureMessage");
                return;
            }
            NetworkSecret = string.Empty;
            ExternalSecret = string.Empty;
            Status = DebugEnabled && FullDebugEnabled
                ? UiStrings.Get("SettingsSavedFullLogMessage")
                : DebugEnabled
                ? UiStrings.Get("SettingsSavedDebugMessage")
                : UiStrings.Get("SettingsSavedMessage");
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            Status = $"{UserFacingErrorMapper.GetMessage(UserFacingFailure.Configuration)} {exception.Message}";
        }
    }

    /// <summary>Generates and validates a UI locale cache through the explicitly selected active profile.</summary>
    [RelayCommand]
    private async Task GenerateUiTranslationAsync()
    {
        string language = UserLanguage;
        if (language.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            Status = UiStrings.Get("EnglishSourceCatalogMessage");
            return;
        }

        IsGeneratingLocale = true;
        Status = UiStrings.Get("GeneratingUiTranslationMessage");
        try
        {
            ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
            var resolver = new ProfileResolver(configuration, new DpapiSecretVault());
            ProfileResolution resolution = resolver.Resolve(SelectedLocalizationProfile, exact: true);
            if (resolution.Kind is ProfileResolutionKind.Failed or ProfileResolutionKind.UserConfirmationRequired
                || resolution.Profile is null
                || resolution.Connection is null)
            {
                throw new InvalidOperationException(resolution.Status);
            }

            ModelProfile profile = resolution.Profile;
            ConnectionDefinition connection = resolution.Connection;
            SelectedLocalizationProfile = profile.Id;

            int contextSize = profile.ProviderOptions.TryGetValue("num_ctx", out object? context) && int.TryParse(context?.ToString(), out int parsed) ? parsed : 8192;
            var settings = new TextTransformationSettings(connection.Endpoint, profile.Model, profile.Temperature, profile.Timeout, contextSize, ThinkingMode.Off);
            const string instruction = "Translate every JSON string value into the requested UI language. Preserve every JSON key and every placeholder such as {name} exactly. Return one JSON object only, with no Markdown or commentary.";
            var service = new MafTextTransformationService(
                _ => aiClientFactory.Create(connection, profile),
                connection.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase),
                maxOutputTokens: connection.Provider == "openai-account" ? null : 16_384,
                honorConfiguredTimeout: connection.Provider == "openai-account");
            string cachePath = Path.Combine(UserConfiguration.GetUserDataDirectory(), "locales", language + ".json");
            var catalog = new LocalizationCatalog(EnglishStringCatalog.Values);
            var generator = new LocaleCatalogGenerator(catalog);
            LocaleCatalogGenerationResult generation = await generator.GenerateInBatchesAsync(
                cachePath,
                Math.Clamp(contextSize / 256, 2, 24),
                async (batch, cancellationToken) => ExtractJsonObject(await service.TransformAsync(
                    new TextTransformationRequest($"Target BCP-47 language: {language}\n\nSource catalog:\n{JsonSerializer.Serialize(batch)}", instruction, settings),
                    cancellationToken)),
                CancellationToken.None);
            if (!generation.Succeeded)
            {
                Status = generation.Status == LocaleCatalogGenerationStatus.InvalidCatalog
                    ? UiStrings.Get("LocaleCatalogInvalidMessage")
                    : UiStrings.Get("LocaleProviderUnavailableMessage");
                return;
            }
            UiTranslationGenerated?.Invoke(this, new UiTranslationGeneratedEventArgs(language, PreferEnglishUi));
            Status = PreferEnglishUi
                ? UiStrings.Get("LocaleCacheCreatedEnglishMessage")
                : UiStrings.Get("LocaleCacheCreatedMessage");
        }
        catch (Exception)
        {
            Status = UiStrings.Get("LocaleProviderUnavailableMessage");
        }
        finally { IsGeneratingLocale = false; }
    }

    private static string ExtractJsonObject(string response)
    {
        int first = response.IndexOf('{');
        int last = response.LastIndexOf('}');
        return first >= 0 && last > first ? response[first..(last + 1)] : response;
    }

    /// <summary>Opens the local directory that holds the optional diagnostic log.</summary>
    [RelayCommand]
    private void OpenDebugFolder()
    {
        try
        {
            string directory = UserConfiguration.GetLocalUserDataDirectory();
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo { FileName = directory, UseShellExecute = true });
            Status = UiStrings.Get("DebugFolderOpenedMessage");
        }
        catch (Exception)
        {
            Status = UserFacingErrorMapper.GetMessage(UserFacingFailure.Configuration);
        }
    }
}

/// <summary>Provides the profile information displayed in the read-only Settings overview.</summary>
public sealed record ProfileSummary(string Id, string ConnectionCategory, string Model, bool IsActive);
