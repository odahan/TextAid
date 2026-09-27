using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextAid.AI;
using TextAid.Core;

namespace TextAid.App;

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
    [ObservableProperty] private string status = "Load the models installed in your local Ollama instance.";
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

    public ObservableCollection<string> Models { get; } = [];
    public IReadOnlyList<ThinkingMode> ThinkingModes { get; } = Enum.GetValues<ThinkingMode>();
    public event EventHandler? Saved;
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
            Status = models.Count == 0 ? "Ollama is available, but no local models were found. Install one with 'ollama pull <model>'." : "Choose a local model, then save your settings.";
        }
        catch
        {
            Models.Clear();
            Status = "TextAid could not reach Ollama. Check the local endpoint and that Ollama is running.";
        }
        finally { IsLoadingModels = false; }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        ConnectionState = ConnectionTestState.Testing;
        Status = "Testing the local Ollama connection…";
        try
        {
            bool isRunning = await chatClientFactory.TestConnectionAsync(Endpoint.Trim(), CancellationToken.None);
            ConnectionState = isRunning ? ConnectionTestState.Ready : ConnectionTestState.Failed;
            Status = isRunning ? "Ollama is available at this endpoint." : "Ollama did not accept a connection at this endpoint.";
        }
        catch
        {
            ConnectionState = ConnectionTestState.Failed;
            Status = "TextAid could not reach Ollama. Check the endpoint and that Ollama is running.";
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
            NetworkSecret = string.Empty;
            ExternalSecret = string.Empty;
            Status = DebugEnabled && FullDebugEnabled
                ? "Settings saved. Full diagnostic logging is enabled locally; credentials are always redacted."
                : DebugEnabled
                ? "Settings saved. Debug logging is enabled for this session and records technical metadata only."
                : "Settings saved. Debug logging is disabled; no new diagnostic log is created.";
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            Status = UserFacingErrorMapper.GetMessage(UserFacingFailure.Configuration);
        }
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
            Status = "Opened the local TextAid debug-log folder.";
        }
        catch (Exception)
        {
            Status = UserFacingErrorMapper.GetMessage(UserFacingFailure.Configuration);
        }
    }
}
