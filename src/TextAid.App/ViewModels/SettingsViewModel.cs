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

namespace TextAid.App.ViewModels;

/// <summary>Represents the outcome of an explicit Ollama connection check.</summary>
public enum ConnectionTestState
{
    NotTested,
    Testing,
    Ready,
    Failed
}

/// <summary>Edits local, On-premises, and External connection settings.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly OllamaChatClientFactory chatClientFactory = new();

    [ObservableProperty] private string endpoint = "http://127.0.0.1:11434";
    [ObservableProperty] private string? selectedModel;
    [ObservableProperty] private string temperatureText = "0.2";
    [ObservableProperty] private string contextSizeText = "8192";
    [ObservableProperty] private string timeoutSecondsText = "120";
    [ObservableProperty] private ThinkingMode selectedThinking = ThinkingMode.Off;
    [ObservableProperty] private string status = UiStrings.Get("LocalModelsHelp");
    [ObservableProperty] private bool isLoadingModels;
    [ObservableProperty] private ConnectionTestState connectionState = ConnectionTestState.NotTested;
    [ObservableProperty] private string networkEndpoint = string.Empty;
    [ObservableProperty] private string networkModel = string.Empty;
    [ObservableProperty] private string networkSecret = string.Empty;
    [ObservableProperty] private string externalEndpoint = string.Empty;
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

    public ObservableCollection<string> Models { get; } = [];
    public IReadOnlyList<LanguageOption> Languages => LanguageCatalog.Supported;
    public IReadOnlyList<ThinkingMode> ThinkingModes { get; } = Enum.GetValues<ThinkingMode>();
    public ObservableCollection<string> ActiveLocalizationProfiles { get; } = [];
    public ObservableCollection<ProfileSummary> ProfileSummaries { get; } = [];
    public event EventHandler? Saved;
    /// <summary>Raised after a complete, valid UI translation catalog is saved locally.</summary>
    public event EventHandler? UiTranslationGenerated;
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
            ActiveLocalizationProfiles.Clear();
            ProfileSummaries.Clear();
            foreach (ModelProfile profile in configuration.Profiles.Where(profile => configuration.Connections.Any(connection => connection.Id.Equals(profile.ConnectionId, StringComparison.OrdinalIgnoreCase) && connection.IsEnabled))) ActiveLocalizationProfiles.Add(profile.Id);
            foreach (ModelProfile profile in configuration.Profiles)
            {
                ConnectionDefinition connection = configuration.Connections.Single(candidate => candidate.Id.Equals(profile.ConnectionId, StringComparison.OrdinalIgnoreCase));
                ProfileSummaries.Add(new ProfileSummary(profile.Id, connection.Category.ToString(), string.IsNullOrWhiteSpace(profile.Model) ? UiStrings.Get("NoModelConfiguredLabel") : profile.Model, connection.IsEnabled));
            }
            SelectedLocalizationProfile = ActiveLocalizationProfiles.Contains(localization.ProfileId) ? localization.ProfileId : ActiveLocalizationProfiles.FirstOrDefault() ?? "local-default";
            ConnectionDefinition local = configuration.Connections.Single(connection => connection.Category == ConnectionCategory.ThisDeviceOnly);
            LocalEnabled = local.IsEnabled;
            ConnectionDefinition? network = configuration.Connections.FirstOrDefault(connection => connection.Category == ConnectionCategory.OnPremises);
            ModelProfile? networkProfile = network is null ? null : configuration.Profiles.FirstOrDefault(profile => profile.ConnectionId.Equals(network.Id, StringComparison.OrdinalIgnoreCase));
            NetworkEndpoint = network?.Endpoint ?? string.Empty;
            NetworkModel = networkProfile?.Model ?? string.Empty;
            NetworkEnabled = network?.IsEnabled ?? false;
            ConnectionDefinition? external = configuration.Connections.FirstOrDefault(connection => connection.Category == ConnectionCategory.External);
            ModelProfile? externalProfile = external is null ? null : configuration.Profiles.FirstOrDefault(profile => profile.ConnectionId.Equals(external.Id, StringComparison.OrdinalIgnoreCase));
            ExternalEndpoint = external?.Endpoint ?? string.Empty;
            ExternalModel = externalProfile?.Model ?? string.Empty;
            ExternalEnabled = external?.IsEnabled ?? false;
            DebugEnabled = UserConfiguration.LoadDebugMode();
            FullDebugEnabled = UserConfiguration.LoadFullDebugMode();
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
            if (!float.TryParse(TemperatureText, NumberStyles.Float, CultureInfo.InvariantCulture, out float temperature)) throw new ArgumentException("Temperature must be a number between 0 and 2.");
            if (!int.TryParse(ContextSizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int contextSize)) throw new ArgumentException("Context size must be a whole number.");
            if (!int.TryParse(TimeoutSecondsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int timeoutSeconds)) throw new ArgumentException("Timeout must be a whole number of seconds.");
            if (LocalEnabled && string.IsNullOrWhiteSpace(SelectedModel)) throw new ArgumentException("Choose a local Ollama model before saving an active local configuration.");

            UserConfiguration.SaveTransformationSettings(new TextTransformationSettings(Endpoint.Trim(), SelectedModel ?? string.Empty, temperature, TimeSpan.FromSeconds(timeoutSeconds), contextSize, SelectedThinking));
            UserConfiguration.SaveRemoteConnectionSettings(NetworkEndpoint, NetworkModel, NetworkSecret, ExternalEndpoint, ExternalModel, ExternalSecret);
            UserConfiguration.SaveConnectionActivation(LocalEnabled, NetworkEnabled, ExternalEnabled);
            UserConfiguration.SaveDebugMode(DebugEnabled);
            UserConfiguration.SaveFullDebugMode(FullDebugEnabled);
            UserConfiguration.SaveUserPreferences(UserLanguage, PreferredTranslationLanguage, NormalShortcut, TranslationShortcut);
            UserConfiguration.SaveLocalizationPreferences(PreferEnglishUi, SelectedLocalizationProfile);
            NetworkSecret = string.Empty;
            ExternalSecret = string.Empty;
            Status = DebugEnabled && FullDebugEnabled
                ? UiStrings.Get("SettingsSavedFullLogMessage")
                : DebugEnabled
                ? UiStrings.Get("SettingsSavedDebugMessage")
                : UiStrings.Get("SettingsSavedMessage");
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            Status = UserFacingErrorMapper.GetMessage(UserFacingFailure.Configuration);
        }
    }

    /// <summary>Generates and validates a UI locale cache through the explicitly selected active profile.</summary>
    [RelayCommand]
    private async Task GenerateUiTranslationAsync()
    {
        if (UserLanguage.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            Status = UiStrings.Get("EnglishSourceCatalogMessage");
            return;
        }

        IsGeneratingLocale = true;
        Status = UiStrings.Get("GeneratingUiTranslationMessage");
        try
        {
            ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
            ModelProfile profile = configuration.Profiles.Single(candidate => candidate.Id.Equals(SelectedLocalizationProfile, StringComparison.OrdinalIgnoreCase));
            ConnectionDefinition connection = configuration.Connections.Single(candidate => candidate.Id.Equals(profile.ConnectionId, StringComparison.OrdinalIgnoreCase));
            if (!connection.IsEnabled) throw new InvalidOperationException("Choose an active profile for catalog generation.");
            if (!connection.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase)) throw new NotSupportedException("This active profile will support UI catalog generation when its provider is available.");
            if (string.IsNullOrWhiteSpace(profile.Model)) throw new InvalidOperationException("Choose a model for the selected catalog-generation profile.");

            int contextSize = profile.ProviderOptions.TryGetValue("num_ctx", out object? context) && int.TryParse(context?.ToString(), out int parsed) ? parsed : 8192;
            var settings = new TextTransformationSettings(connection.Endpoint, profile.Model, profile.Temperature, profile.Timeout, contextSize, ThinkingMode.Off);
            string source = JsonSerializer.Serialize(EnglishStringCatalog.Values);
            const string instruction = "Translate every JSON string value into the requested UI language. Preserve every JSON key and every placeholder such as {name} exactly. Return one JSON object only, with no Markdown or commentary.";
            var service = new MafTextTransformationService(new OllamaChatClientFactory().Create);
            string cachePath = Path.Combine(UserConfiguration.GetUserDataDirectory(), "locales", UserLanguage + ".json");
            var catalog = new LocalizationCatalog(EnglishStringCatalog.Values);
            var generator = new LocaleCatalogGenerator(catalog);
            LocaleCatalogGenerationResult generation = await generator.GenerateAsync(
                cachePath,
                async cancellationToken => ExtractJsonObject(await service.TransformAsync(
                    new TextTransformationRequest($"Target BCP-47 language: {UserLanguage}\n\nSource catalog:\n{source}", instruction, settings),
                    cancellationToken)),
                CancellationToken.None);
            if (!generation.Succeeded) throw new InvalidOperationException("The generated translation did not preserve the complete TextAid UI catalog. English remains active.");
            UiTranslationGenerated?.Invoke(this, EventArgs.Empty);
            Status = PreferEnglishUi
                ? UiStrings.Get("LocaleCacheCreatedEnglishMessage")
                : UiStrings.Get("LocaleCacheCreatedMessage");
        }
        catch (Exception)
        {
            Status = UiStrings.Get("LocaleGenerationFailureMessage");
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
