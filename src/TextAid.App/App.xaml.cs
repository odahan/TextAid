using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TextAid.Core;
using TextAid.AI;
using TextAid.Platform.Windows;

namespace TextAid.App;

/// <summary>Owns the resident tray lifecycle and one active invocation session.</summary>
public partial class App : Application
{
    private KeyboardHook? hook;
    private TrayIcon? tray;
    private MainWindow? sessionWindow;
    private SettingsWindow? settingsWindow;
    private StartupNoticeWindow? startupNoticeWindow;
    private AboutWindow? aboutWindow;
    private Mutex? instanceMutex;
    private readonly ResultActions resultActions = new(new Win32ResultActions());
    private readonly ITextTransformationService transformationService = new MafTextTransformationService(new OllamaChatClientFactory().Create);
    private bool enabled = true;
    private bool capturing;
    private string? actionsDirectory;
    private DebugSessionLog debugLog = DebugSessionLog.Start(false, false, Path.GetTempPath());

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        instanceMutex = new Mutex(true, @"Local\TextAid.SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
        {
            instanceMutex.Dispose();
            instanceMutex = null;
            Shutdown();
            return;
        }

        UserConfiguration.EnsureCreated();
        ConfigureDebugLog();
        actionsDirectory = UserConfiguration.EnsureActionsDirectory();
        ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
        new ActionLoader(configuration.Profiles.Select(profile => profile.Id), actionsDirectory).Load();
        hook = new KeyboardHook();
        hook.Triggered += (source, hasCopiedText) => Dispatcher.BeginInvoke(new Action(() => _ = CaptureWithFeedbackAsync(source, hasCopiedText)));
        using Stream icon = GetResourceStream(new Uri("pack://application:,,,/TextAid;component/Assets/TextAid.ico")).Stream;
        tray = new TrayIcon(icon);
        tray.Clicked += () => Dispatcher.BeginInvoke(ShowTrayMenu);
        debugLog.Write("application-started");
        Dispatcher.BeginInvoke(new Action(() => _ = CheckInitialProviderAsync()));
    }

    private async Task CheckInitialProviderAsync()
    {
        try
        {
            ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
            ConnectionDefinition localConnection = configuration.Connections.Single(connection => connection.Category == ConnectionCategory.ThisDeviceOnly);
            if (!localConnection.IsEnabled) return;
            TextTransformationSettings settings = UserConfiguration.LoadTransformationSettings();
            var factory = new OllamaChatClientFactory();
            bool running = await factory.TestConnectionAsync(settings.Endpoint, CancellationToken.None);
            if (!running || (await factory.GetLocalModelNamesAsync(settings.Endpoint, CancellationToken.None)).Count == 0) ShowStartupNotice();
        }
        catch
        {
            debugLog.Write("startup-provider-check-failed");
            ShowStartupNotice();
        }
    }

    private void ShowStartupNotice()
    {
        if (startupNoticeWindow is { IsVisible: true }) return;
        if (settingsWindow is not null || aboutWindow is not null) return;
        startupNoticeWindow = new StartupNoticeWindow();
        startupNoticeWindow.SettingsRequested += (_, _) => ShowSettings();
        startupNoticeWindow.Closed += (_, _) => startupNoticeWindow = null;
        startupNoticeWindow.ShowDialog();
    }

    private async Task CaptureWithFeedbackAsync(nint source, bool hasCopiedText)
    {
        try
        {
            await CaptureAsync(source, hasCopiedText);
        }
        catch (Exception exception)
        {
            debugLog.WriteException("capture", exception);
            tray?.ShowError((string)FindResource("SessionError"));
        }
    }

    /// <summary>Opens a transformation session from a verified copy gesture or as an empty manual entry.</summary>
    private async Task CaptureAsync(nint source, bool hasCopiedText)
    {
        if (!enabled || capturing || sessionWindow is not null) return;
        capturing = true;
        try
        {
            string? text;
            bool failed = false;
            string? failureMessage = null;
            if (hasCopiedText)
            {
                try { text = await ClipboardReader.ReadUnicodeTextAsync(CancellationToken.None); }
                catch (InvalidOperationException exception)
                {
                    debugLog.WriteException("clipboard-read", exception);
                    text = string.Empty;
                    failed = true;
                    failureMessage = await GetRecoveryMessageAsync(UserFacingFailure.Clipboard);
                }
            }
            else text = string.Empty;
            debugLog.Write("capture-completed", ("hasCopiedText", hasCopiedText), ("inputLength", text?.Length ?? 0));
            debugLog.WriteFullText("clipboard-input", text);
            bool noText = !failed && string.IsNullOrWhiteSpace(text);
            nint safeSource = noText ? 0 : source;
            var session = new InvocationSession(safeSource, text ?? string.Empty)
            {
                State = failed ? InvocationState.Failed : InvocationState.Ready
            };
            string status = failed ? failureMessage! : noText ? (string)FindResource("EnterTextMessage") : (string)FindResource("ReadyToProcessMessage");
            ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
            IReadOnlyList<ActionDefinition> actions = new ActionLoader(configuration.Profiles.Select(profile => profile.Id), actionsDirectory).Load();
            bool isFullLogActive = UserConfiguration.LoadDebugMode() && UserConfiguration.LoadFullDebugMode();
            sessionWindow = new MainWindow(session, actions, UserConfiguration.LoadActionPresetIds(), ShowTrayMenu, ReplaceOutput, CopyResult, StartTransformation, EditInstructions, ResetSession, status, isFullLogActive);
            sessionWindow.Closed += (_, _) => sessionWindow = null;
            sessionWindow.Show();
            sessionWindow.Activate();
        }
        finally { capturing = false; }
    }

    private async Task TransformAsync(InvocationSession session, MainWindow window, string inputText, ActionDefinition action, string? supplementaryInstructions)
    {
        ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
        var resolver = new ProfileResolver(configuration, new DpapiSecretVault());
        ProfileResolution resolution = resolver.Resolve(action.ProfileId);
        await TransformWithResolutionAsync(session, window, inputText, action, supplementaryInstructions, resolver, resolution);
    }

    private async Task TransformWithResolutionAsync(InvocationSession session, MainWindow window, string inputText, ActionDefinition action, string? supplementaryInstructions, ProfileResolver resolver, ProfileResolution resolution)
    {
        try
        {
            if (resolution.Kind == ProfileResolutionKind.Failed || resolution.Profile is null || resolution.Connection is null)
            {
                debugLog.Write("configuration-rejected", ("requestedCategory", resolution.RequestedCategory));
                window.ShowFailure(await GetRecoveryMessageAsync(UserFacingFailure.Configuration, resolution.Status));
                return;
            }

            if (resolution.Kind == ProfileResolutionKind.UserConfirmationRequired && !ConfirmDowngrade(window, resolution.Status))
            {
                window.ShowFailure("The configuration downgrade was cancelled.");
                return;
            }

            window.ShowTransforming(resolution.Status);
            debugLog.Write("transformation-started", ("category", resolution.Connection.Category), ("inputLength", inputText.Length));
            if (!resolution.Connection.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException($"The {resolution.Connection.Provider} provider will be available in LOT-009.");
            TextTransformationSettings settings = CreateTransformationSettings(resolution.Profile, resolution.Connection);

            if (action.TemperatureOverride is not null) settings = settings with { Temperature = action.TemperatureOverride.Value };
            string instruction = TemplateRenderer.Render(action.PromptTemplate, inputText);
            if (!string.IsNullOrWhiteSpace(supplementaryInstructions))
                instruction = $"{instruction}\n\nAdditional user instructions for this invocation:\n{supplementaryInstructions}";
            debugLog.WriteFullText("transformation-input", inputText);
            debugLog.WriteFullText("transformation-prompt", instruction);
            string output = await transformationService.TransformAsync(
                new TextTransformationRequest(
                    inputText,
                    instruction,
                    settings),
                session.Cancellation.Token);
            if (!window.IsLoaded) return;
            debugLog.Write("transformation-completed", ("outputLength", output.Length));
            debugLog.WriteFullText("transformation-output", output);
            window.ShowResult(output);
        }
        catch (OperationCanceledException) when (session.Cancellation.IsCancellationRequested)
        {
            // Closing the session is a normal cancellation path.
            debugLog.Write("transformation-cancelled");
        }
        catch (OperationCanceledException)
        {
            await OfferProviderFailureDowngradeAsync(session, window, inputText, action, supplementaryInstructions, resolver, resolution, (string)FindResource("TransformationTimeoutError"));
        }
        catch (Exception exception)
        {
            debugLog.WriteException("transformation", exception);
            UserFacingFailure failureKind = exception is InvalidOperationException invalid && invalid.Message.Contains("model", StringComparison.OrdinalIgnoreCase)
                ? UserFacingFailure.Model
                : UserFacingFailure.Provider;
            string failure = await GetRecoveryMessageAsync(failureKind);
            await OfferProviderFailureDowngradeAsync(session, window, inputText, action, supplementaryInstructions, resolver, resolution, failure);
        }
    }

    /// <summary>Starts a user-requested transformation from the editable session input.</summary>
    private void StartTransformation(MainWindow window, InvocationSession session)
    {
        if (string.IsNullOrWhiteSpace(session.InputText) || session.State == InvocationState.Transforming) return;
        ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
        ActionDefinition? action = new ActionLoader(configuration.Profiles.Select(profile => profile.Id), actionsDirectory).Load().FirstOrDefault(candidate => candidate.Id.Equals(session.ActionId, StringComparison.OrdinalIgnoreCase));
        if (action is null)
        {
            debugLog.Write("user-facing-failure", ("category", UserFacingFailure.Configuration));
            window.ShowFailure("The selected action is no longer available.");
            return;
        }

        if (action.AskForUserInstructions && string.IsNullOrWhiteSpace(session.SupplementaryInstructions) && !TryCollectInstructions(window, session)) return;
        string inputSnapshot = session.InputText;
        string? instructionsSnapshot = session.SupplementaryInstructions;
        session.State = InvocationState.Transforming;
        window.ShowTransforming((string)FindResource("TransformingMessage"));
        _ = TransformAsync(session, window, inputSnapshot, action, instructionsSnapshot);
    }

    private static TextTransformationSettings CreateTransformationSettings(ModelProfile profile, ConnectionDefinition connection)
    {
        int contextSize = profile.ProviderOptions.TryGetValue("num_ctx", out object? value) && int.TryParse(value?.ToString(), out int parsed) ? parsed : 8192;
        ThinkingMode thinking = profile.ProviderOptions.TryGetValue("think", out object? think) && Enum.TryParse(think?.ToString(), true, out ThinkingMode parsedThinking) ? parsedThinking : ThinkingMode.Off;
        return new TextTransformationSettings(connection.Endpoint, profile.Model, profile.Temperature, profile.Timeout, contextSize, thinking);
    }

    private async Task OfferProviderFailureDowngradeAsync(InvocationSession session, MainWindow window, string inputText, ActionDefinition action, string? supplementaryInstructions, ProfileResolver resolver, ProfileResolution usedResolution, string providerFailure)
    {
        if (usedResolution.Connection is null)
        {
            window.ShowFailure(providerFailure);
            return;
        }

        ProfileResolution downgrade = resolver.OfferDowngradeAfterProviderFailure(usedResolution.Connection.Category, providerFailure);
        if (downgrade.Kind == ProfileResolutionKind.UserConfirmationRequired && ConfirmDowngrade(window, downgrade.Status))
        {
            await TransformWithResolutionAsync(session, window, inputText, action, supplementaryInstructions, resolver, downgrade);
            return;
        }

        window.ShowFailure(downgrade.Kind == ProfileResolutionKind.Failed ? downgrade.Status : providerFailure);
    }

    private static bool ConfirmDowngrade(Window owner, string message) =>
        MessageBox.Show(owner, message, "TextAid", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    /// <summary>Opens the optional per-invocation instructions editor without changing action data.</summary>
    private void EditInstructions(MainWindow window, InvocationSession session) => TryCollectInstructions(window, session);

    /// <summary>Prepares the existing window for a separate manual transformation.</summary>
    private static void ResetSession(MainWindow window, InvocationSession session) => window.ResetForNewInput();

    private static bool TryCollectInstructions(MainWindow owner, InvocationSession session)
    {
        var dialog = new InstructionsWindow(session.SupplementaryInstructions) { Owner = owner };
        if (dialog.ShowDialog() != true) return false;
        session.SupplementaryInstructions = dialog.Instructions;
        return true;
    }

    private void CopyResult(MainWindow window, InvocationSession session)
    {
        if (!TryCopyToClipboard(session.OutputText, window)) return;
        resultActions.Copy();
        session.State = InvocationState.Copied;
        window.Close();
    }

    private void ReplaceOutput(MainWindow window, InvocationSession session)
    {
        if (!TryCopyToClipboard(session.OutputText, window)) return;

        ReplaceResult outcome = resultActions.TryReplace(session.SourceWindow);
        if (outcome == ReplaceResult.Replaced)
        {
            session.State = InvocationState.Replaced;
            window.Close();
            return;
        }

        window.ShowFailure((string)FindResource(outcome switch
        {
            ReplaceResult.InvalidTarget => "InvalidTargetError",
            ReplaceResult.FocusFailed => "FocusFailedError",
            ReplaceResult.ModifiersStillPressed => "ModifiersPressedError",
            ReplaceResult.PasteFailed => "PasteFailedError",
            _ => "PasteFailedError"
        }));
        debugLog.Write("user-facing-failure", ("category", outcome is ReplaceResult.InvalidTarget or ReplaceResult.FocusFailed ? UserFacingFailure.SourceWindow : UserFacingFailure.Paste));
    }

    private bool TryCopyToClipboard(string? text, MainWindow window)
    {
        if (string.IsNullOrEmpty(text)) return false;
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (Exception)
        {
            debugLog.Write("clipboard-write-failed");
            debugLog.Write("user-facing-failure", ("category", UserFacingFailure.Clipboard));
            window.ShowFailure((string)FindResource("ClipboardWriteError"));
            return false;
        }
    }

    private void ShowTrayMenu()
    {
        string Label(string key) => (string)FindResource(key);
        var itemStyle = (Style)FindResource("TrayMenuItemStyle");
        var separatorStyle = (Style)FindResource("TraySeparatorStyle");
        var menu = new ContextMenu
        {
            Placement = PlacementMode.MousePoint,
            Style = (Style)FindResource("TrayContextMenuStyle")
        };
        var toggle = new MenuItem { Header = Label(enabled ? "EnabledLabel" : "DisabledLabel"), Style = itemStyle };
        toggle.Click += (_, _) => enabled = !enabled;
        var settings = new MenuItem { Header = Label("SettingsLabel"), Style = itemStyle };
        settings.Click += (_, _) => ShowSettings();
        var open = new MenuItem { Header = Label("OpenLabel"), Style = itemStyle };
        open.Click += (_, _) => _ = CaptureWithFeedbackAsync(0, hasCopiedText: false);
        var about = new MenuItem { Header = Label("AboutLabel"), Style = itemStyle };
        about.Click += (_, _) => ShowAbout();
        var exit = new MenuItem { Header = Label("ExitLabel"), Style = itemStyle };
        exit.Click += (_, _) => Shutdown();
        menu.Items.Add(toggle);
        menu.Items.Add(new Separator { Style = separatorStyle });
        menu.Items.Add(open);
        menu.Items.Add(settings);
        menu.Items.Add(about);
        menu.Items.Add(new Separator { Style = separatorStyle });
        menu.Items.Add(exit);
        menu.IsOpen = true;
    }

    private void ShowSettings()
    {
        if (settingsWindow is { IsVisible: true })
        {
            settingsWindow.Activate();
            settingsWindow.Topmost = true;
            settingsWindow.Topmost = false;
            settingsWindow.Focus();
            return;
        }

        if (startupNoticeWindow is not null || aboutWindow is not null) return;

        settingsWindow = new SettingsWindow();
        settingsWindow.FullLogActivityChanged += active => sessionWindow?.SetFullLogActive(active);
        settingsWindow.SettingsSaved += (_, _) =>
        {
            ConfigureDebugLog();
            sessionWindow?.SetFullLogActive(UserConfiguration.LoadDebugMode() && UserConfiguration.LoadFullDebugMode());
        };
        settingsWindow.Closed += (_, _) =>
        {
            sessionWindow?.SetFullLogActive(UserConfiguration.LoadDebugMode() && UserConfiguration.LoadFullDebugMode());
            settingsWindow = null;
        };
        settingsWindow.ShowDialog();
    }

    private void ShowAbout()
    {
        if (aboutWindow is { IsVisible: true })
        {
            aboutWindow.Activate();
            aboutWindow.Focus();
            return;
        }

        if (settingsWindow is not null || startupNoticeWindow is not null) return;

        aboutWindow = new AboutWindow();
        aboutWindow.Closed += (_, _) => aboutWindow = null;
        aboutWindow.ShowDialog();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        debugLog.Write("application-stopped");
        debugLog.Dispose();
        hook?.Dispose();
        tray?.Dispose();
        instanceMutex?.ReleaseMutex();
        instanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void ConfigureDebugLog()
    {
        debugLog.Dispose();
        debugLog = DebugSessionLog.Start(UserConfiguration.LoadDebugMode(), UserConfiguration.LoadFullDebugMode(), UserConfiguration.GetLocalUserDataDirectory());
    }

    /// <summary>Returns a short model-generated recovery suggestion when an eligible Ollama profile is available.</summary>
    private async Task<string> GetRecoveryMessageAsync(UserFacingFailure failure, string? context = null)
    {
        string fallback = UserFacingErrorMapper.GetMessage(failure);
        debugLog.Write("user-facing-failure", ("category", failure));
        string prefix = string.IsNullOrWhiteSpace(context) ? fallback : $"{context} {fallback}";
        if (!TryGetGuidanceSettings(out TextTransformationSettings? settings) || settings is null) return prefix;

        try
        {
            const string instruction = "Give one brief, practical recovery step for this TextAid problem category. Do not request, repeat, or infer user text, secrets, API keys, endpoints, prompts, or exception details. Return one sentence only.";
            string guidance = await transformationService.TransformAsync(
                new TextTransformationRequest(failure.ToString(), instruction, settings),
                CancellationToken.None);
            string singleSentence = guidance.ReplaceLineEndings(" ").Trim();
            if (singleSentence.Length > 240) singleSentence = singleSentence[..240].TrimEnd() + "…";
            debugLog.Write("recovery-guidance-generated", ("failure", failure));
            return string.IsNullOrWhiteSpace(singleSentence) ? prefix : $"{prefix} Suggested next step: {singleSentence}";
        }
        catch (Exception exception)
        {
            debugLog.WriteException("recovery-guidance", exception);
            return prefix;
        }
    }

    /// <summary>Selects the active local Ollama configuration, or the sole active Ollama configuration, for recovery guidance.</summary>
    private static bool TryGetGuidanceSettings(out TextTransformationSettings? settings)
    {
        settings = null;
        try
        {
            ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
            var candidates = configuration.Connections
                .Where(connection => connection.IsEnabled && connection.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
                .Select(connection => new { Connection = connection, Profile = configuration.Profiles.FirstOrDefault(profile => profile.ConnectionId.Equals(connection.Id, StringComparison.OrdinalIgnoreCase)) })
                .Where(candidate => candidate.Profile is not null && !string.IsNullOrWhiteSpace(candidate.Profile.Model) && Uri.TryCreate(candidate.Connection.Endpoint, UriKind.Absolute, out _))
                .ToArray();
            var selected = candidates.FirstOrDefault(candidate => candidate.Connection.Category == ConnectionCategory.ThisDeviceOnly)
                ?? (candidates.Length == 1 ? candidates[0] : null);
            if (selected is null || selected.Profile is null) return false;
            settings = CreateTransformationSettings(selected.Profile, selected.Connection);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
