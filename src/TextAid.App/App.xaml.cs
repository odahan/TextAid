using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TextAid.Core;
using TextAid.AI;
using TextAid.Platform.Windows;
using TextAid.App.Views;
using TextAid.App.Localization;

namespace TextAid.App;

/// <summary>Owns the resident tray lifecycle and one active invocation session.</summary>
public partial class App : Application
{
    private const int DefaultActionOutputTokenBudget = 16_384;
    private KeyboardHook? hook;
    private TrayIcon? tray;
    private MainWindow? sessionWindow;
    private SettingsWindow? settingsWindow;
    private StartupNoticeWindow? startupNoticeWindow;
    private AboutWindow? aboutWindow;
    private ActionsWindow? actionsWindow;
    private TranslationReviewWindow? translationReviewWindow;
    private Mutex? instanceMutex;
    private readonly ResultActions resultActions = new(new Win32ResultActions());
    private readonly ITextTransformationService transformationService = new MafTextTransformationService(new OllamaChatClientFactory().Create);
    private readonly OllamaChatClientFactory ollamaChatClientFactory = new();
    private readonly AiClientFactory chatClientFactory = new();
    private bool enabled = true;
    private bool capturing;
    private string? actionsDirectory;
    private string? localeFallbackNotice;
    private bool isUiTranslationRequired;
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

        ConfigurationSnapshot configuration;
        try
        {
            UserConfiguration.EnsureCreated();
            configuration = UserConfiguration.LoadConfiguration();
            _ = UserConfiguration.LoadDebugMode();
            _ = UserConfiguration.LoadFullDebugMode();
            _ = UserConfiguration.LoadLocalizationPreferences();
            _ = UserConfiguration.LoadActionPresetIds();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.Text.Json.JsonException or IOException or ArgumentException)
        {
            ShowConfigurationRecovery();
            Shutdown();
            return;
        }
        ConfigureDebugLog();
        actionsDirectory = UserConfiguration.EnsureActionsDirectory();
        ApplyCachedLocale(configuration.UserLanguage, UserConfiguration.LoadLocalizationPreferences().PreferEnglishUi);
        new ActionLoader(configuration.Profiles.Select(profile => profile.Id), actionsDirectory).Load();
        ConfigureKeyboardHook(configuration);
        using Stream icon = GetResourceStream(new Uri("pack://application:,,,/TextAid;component/Assets/TextAid.ico")).Stream;
        tray = new TrayIcon(icon);
        tray.Clicked += () => Dispatcher.BeginInvoke(ShowTrayMenu);
        if (localeFallbackNotice is not null) tray.ShowInfo(localeFallbackNotice);
        debugLog.Write("application-started");
        Dispatcher.BeginInvoke(new Action(() => _ = CheckInitialProviderAsync()));
    }

    /// <summary>Offers the configuration file for manual repair when startup cannot load it.</summary>
    private static void ShowConfigurationRecovery()
    {
        string path = Path.Combine(UserConfiguration.GetUserDataDirectory(), "config.json");
        MessageBoxResult choice = MessageBox.Show(
            $"TextAid cannot load its configuration.\n\n{path}\n\nOpen it in Notepad for repair?",
            "TextAid configuration error",
            MessageBoxButton.YesNo,
            MessageBoxImage.Error);
        if (choice != MessageBoxResult.Yes) return;
        try
        {
            var startInfo = new ProcessStartInfo("notepad.exe") { UseShellExecute = true };
            startInfo.ArgumentList.Add(path);
            Process.Start(startInfo);
        }
        catch (Exception) { }
    }

    private Task CheckInitialProviderAsync()
    {
        try
        {
            ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
            if (!configuration.HasEnabledConfiguredProvider()) ShowStartupNotice();
        }
        catch
        {
            debugLog.Write("startup-provider-check-failed");
            ShowStartupNotice();
        }

        return Task.CompletedTask;
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

    private async Task CaptureWithFeedbackAsync(nint source, bool hasCopiedText, InvocationShortcut shortcut = InvocationShortcut.Choose)
    {
        try
        {
            await CaptureAsync(source, hasCopiedText, shortcut);
        }
        catch (Exception exception)
        {
            debugLog.WriteException("capture", exception);
            tray?.ShowError((string)FindResource("SessionError"));
        }
    }

    /// <summary>Opens a transformation session from a verified copy gesture or as an empty manual entry.</summary>
    private async Task CaptureAsync(nint source, bool hasCopiedText, InvocationShortcut shortcut)
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
            if (shortcut == InvocationShortcut.Translate)
            {
                session.ActionId = "translate";
                status = (string)FindResource("TranslatingCapturedTextMessage");
            }
            bool isFullLogActive = UserConfiguration.LoadDebugMode() && UserConfiguration.LoadFullDebugMode();
            sessionWindow = new MainWindow(session, actions, UserConfiguration.LoadActionPresetIds(), ShowTrayMenu, ReplaceOutput, CopyResult, (window, invocation) => StartTransformation(window, invocation), EditInstructions, ResetSession, StartFreeTransformation, status, isFullLogActive, isUiTranslationRequired);
            sessionWindow.Closed += (_, _) => sessionWindow = null;
            sessionWindow.Show();
            sessionWindow.ActivateForUserInput();
            if (!failed && !string.IsNullOrWhiteSpace(session.InputText))
            {
                ActionDefinition translation = actions.Single(action => action.Id.Equals("translate", StringComparison.OrdinalIgnoreCase));
                _ = DetectCapturedLanguageAsync(session, sessionWindow, translation);
            }
            if (shortcut == InvocationShortcut.Translate && !failed && !string.IsNullOrWhiteSpace(session.InputText)) StartTransformation(sessionWindow, session);
        }
        finally { capturing = false; }
    }

    /// <summary>Starts shared language detection using the captured action snapshot without reloading action files.</summary>
    private async Task DetectCapturedLanguageAsync(InvocationSession session, MainWindow window, ActionDefinition translation)
    {
        try
        {
            await session.DetectInputLanguageAsync(async (input, token) =>
            {
                ProfileResolution resolution = await Task.Run(() =>
                {
                    ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
                    return new ProfileResolver(configuration, new DpapiSecretVault()).Resolve(translation.ProfileId);
                }, token);
                token.ThrowIfCancellationRequested();
                if (resolution.Kind == ProfileResolutionKind.UserConfirmationRequired && !ConfirmDowngrade(window, resolution.Status))
                {
                    throw new InvalidOperationException("The language detection configuration downgrade was cancelled.");
                }
                if (resolution.Connection is not null && session.State != InvocationState.Transforming)
                {
                    window.SetActiveConnection(resolution.Connection.Category);
                }
                return await DetectLanguageWithResolutionAsync(input, resolution, token);
            });
        }
        catch (OperationCanceledException)
        {
            debugLog.Write("language-detection-cancelled");
        }
        catch (Exception exception)
        {
            debugLog.WriteException("language-detection", exception);
        }
    }

    /// <summary>Runs provider setup, inference, and response parsing entirely off the UI thread.</summary>
    private async Task<string?> DetectLanguageWithResolutionAsync(string input, ProfileResolution resolution, CancellationToken token)
    {
        string? language = await Task.Run(async () =>
        {
            if (resolution.Kind == ProfileResolutionKind.Failed || resolution.Connection is null || resolution.Profile is null)
            {
                throw new InvalidOperationException(resolution.Status);
            }
            var transformation = new MafTextTransformationService(
                _ => chatClientFactory.Create(resolution.Connection, resolution.Profile),
                resolution.Connection.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase),
                maxOutputTokens: 128);
            var detection = new AiLanguageDetectionService(transformation);
            return await detection.DetectAsync(input, CreateTransformationSettings(resolution.Profile, resolution.Connection), token);
        }, token);
        token.ThrowIfCancellationRequested();
        debugLog.Write("language-detection-completed", ("identified", language is not null));
        return language;
    }

    private async Task TransformAsync(InvocationSession session, MainWindow window, string inputText, ActionDefinition action, string? supplementaryInstructions, int generation)
    {
        ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
        var resolver = new ProfileResolver(configuration, new DpapiSecretVault());
        ProfileResolution resolution = resolver.Resolve(action.ProfileId);
        await TransformWithResolutionAsync(session, window, inputText, action, supplementaryInstructions, resolver, resolution, generation);
    }

    private async Task TransformWithResolutionAsync(InvocationSession session, MainWindow window, string inputText, ActionDefinition action, string? supplementaryInstructions, ProfileResolver resolver, ProfileResolution resolution, int generation)
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
                window.ShowFailure((string)FindResource("ConfigurationDowngradeCancelledMessage"));
                return;
            }

            window.SetActiveConnection(resolution.Connection.Category);
            TextTransformationSettings settings = CreateTransformationSettings(resolution.Profile, resolution.Connection);

            window.ShowTransforming((string)FindResource("DetectingInputLanguageMessage"));
            string? detectedInputLanguage = await session.DetectInputLanguageAsync(
                (input, token) => DetectLanguageWithResolutionAsync(input, resolution, token)).WaitAsync(session.Cancellation.Token);
            if (!window.IsLoaded || generation != session.Generation) return;
            if (!session.IsFreeMode && action.IsReserved && session.OutputLanguage.Equals("Unchanged", StringComparison.OrdinalIgnoreCase))
            {
                if (detectedInputLanguage is null)
                {
                    window.ShowFailure((string)FindResource("InputLanguageUndeterminedMessage"));
                    return;
                }
                ConfigurationSnapshot routingConfiguration = UserConfiguration.LoadConfiguration();
                session.SelectAutomaticTranslationDestination(
                    detectedInputLanguage, routingConfiguration.UserLanguage, routingConfiguration.PreferredTranslationLanguage);
                window.RefreshOutputLanguage();
            }

            if (resolution.Connection.Category == ConnectionCategory.ThisDeviceOnly
                && resolution.Connection.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase)
                && !await ollamaChatClientFactory.IsModelLoadedAsync(settings, session.Cancellation.Token))
            {
                window.ShowTransforming((string)FindResource("WarmingUpLocalModelMessage"));
                debugLog.Write("local-model-warmup-started", ("model", settings.Model));
                await ollamaChatClientFactory.WarmupAsync(settings, session.Cancellation.Token);
                debugLog.Write("local-model-warmup-completed", ("model", settings.Model));
            }

            window.ShowTransforming(resolution.Status);
            debugLog.Write("transformation-started", ("category", resolution.Connection.Category), ("inputLength", inputText.Length));

            if (action.TemperatureOverride is not null) settings = settings with { Temperature = action.TemperatureOverride.Value };
            string instruction = session.IsFreeMode
                ? "Respond directly to the user message."
                : TemplateRenderer.Render(action.PromptTemplate, inputText, supplementaryInstructions);
            string outputLanguageCode = string.IsNullOrWhiteSpace(session.OutputLanguage) ? "Unchanged" : session.OutputLanguage;
            if (!outputLanguageCode.Equals("Unchanged", StringComparison.OrdinalIgnoreCase))
            {
                LanguageOption outputLanguage = LanguageCatalog.Supported.Single(option => option.Code.Equals(outputLanguageCode, StringComparison.OrdinalIgnoreCase));
                instruction = $"{instruction}\n\nGENERATION LANGUAGE = {outputLanguage.EnglishName} ({outputLanguage.Code}). Write the entire result in this language. Do not return the result in the source language unless it is {outputLanguage.EnglishName}.";
            }
            else
            {
                instruction = detectedInputLanguage is null
                    ? $"{instruction}\n\nWrite the result in the same language or languages as the input. Do not translate it."
                    : $"{instruction}\n\nGENERATION LANGUAGE = {detectedInputLanguage}. This is the detected language tag of the input. Write the entire result in this language; do not translate it into another language.";
            }
            if (session.MarkdownOutputEnabled)
                instruction = $"{instruction}\n\nFormat the entire result as GitHub-flavored Markdown. Use Markdown only; do not wrap it in a fenced code block unless the requested content itself is code.";
            else
                instruction = $"{instruction}\n\nReturn plain text only. Do not use Markdown syntax, including headings, emphasis markers, lists, tables, links, block quotes, or fenced code blocks.";
            instruction = $"{instruction}\n\nEMOJI POLICY: Preserve every emoji from the input exactly as it appears. Do not introduce emojis, icons, or decorative symbols unless the user explicitly requests them in this invocation. Do not remove or alter input emojis unless the user explicitly asks you to do so.";
            if (!session.IsFreeMode && !string.IsNullOrWhiteSpace(supplementaryInstructions) && !TemplateRenderer.ContainsAnswerPlaceholder(action.PromptTemplate))
                instruction = $"{instruction}\n\nAdditional user instructions for this invocation:\n{supplementaryInstructions}";
            debugLog.WriteFullText("transformation-input", inputText);
            debugLog.WriteFullText("transformation-prompt", instruction);
            var resolvedTransformationService = new MafTextTransformationService(
                _ => chatClientFactory.Create(resolution.Connection, resolution.Profile),
                resolution.Connection.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase),
                DefaultActionOutputTokenBudget);
            string output = await resolvedTransformationService.TransformAsync(
                new TextTransformationRequest(
                    inputText,
                    instruction,
                    settings),
                session.Cancellation.Token);
            if (!window.IsLoaded || !session.TryCompleteGeneration(generation, output)) return;
            session.SupplementaryInstructions = null;
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
            if (generation == session.Generation) await OfferProviderFailureDowngradeAsync(session, window, inputText, action, supplementaryInstructions, resolver, resolution, (string)FindResource("TransformationTimeoutError"), generation);
        }
        catch (Exception exception)
        {
            debugLog.WriteException("transformation", exception);
            System.ClientModel.ClientResultException? clientException = FindClientResultException(exception);
            if (clientException is not null) debugLog.Write("external-provider-response", ("httpStatus", clientException.Status));
            UserFacingFailure failureKind = exception is InvalidOperationException invalid && invalid.Message.Contains("model", StringComparison.OrdinalIgnoreCase)
                ? UserFacingFailure.Model
                : UserFacingFailure.Provider;
            string failure = resolution.Connection?.Category == ConnectionCategory.External && failureKind != UserFacingFailure.Model
                ? GetExternalProviderFailureMessage(exception)
                : await GetRecoveryMessageAsync(failureKind);
            if (generation == session.Generation) await OfferProviderFailureDowngradeAsync(session, window, inputText, action, supplementaryInstructions, resolver, resolution, failure, generation);
        }
    }

    /// <summary>Maps safe OpenAI-compatible HTTP status categories without exposing response text or credentials.</summary>
    private string GetExternalProviderFailureMessage(Exception exception)
    {
        System.ClientModel.ClientResultException? clientException = FindClientResultException(exception);
        return clientException?.Status switch
        {
            401 or 403 => (string)FindResource("ExternalAuthenticationFailure"),
            404 => (string)FindResource("ExternalModelUnavailableFailure"),
            429 => (string)FindResource("ExternalRateLimitFailure"),
            400 or 422 => (string)FindResource("ExternalRequestRejectedFailure"),
            _ => (string)FindResource("ExternalProviderRequestFailure")
        };
    }

    private static System.ClientModel.ClientResultException? FindClientResultException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is System.ClientModel.ClientResultException clientException) return clientException;
        }
        return null;
    }

    /// <summary>Starts a user-requested transformation from the editable session input.</summary>
    private void StartTransformation(MainWindow window, InvocationSession session, bool isFreeInvocation = false)
    {
        if (string.IsNullOrWhiteSpace(session.InputText)) return;
        if (!isFreeInvocation) session.UseStandardActionMode();
        ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
        ActionDefinition? action = new ActionLoader(configuration.Profiles.Select(profile => profile.Id), actionsDirectory).Load().FirstOrDefault(candidate => candidate.Id.Equals(session.ActionId, StringComparison.OrdinalIgnoreCase));
        if (action is null)
        {
            debugLog.Write("user-facing-failure", ("category", UserFacingFailure.Configuration));
            window.ShowFailure((string)FindResource("SelectedActionUnavailableMessage"));
            return;
        }

        if (!session.IsFreeMode && action.AskForUserInstructions && string.IsNullOrWhiteSpace(session.SupplementaryInstructions) && !TryCollectInstructions(window, session, action.UserInstructionsQuestion)) return;
        string inputSnapshot = session.InputText;
        string? instructionsSnapshot = session.SupplementaryInstructions;
        int generation = session.StartNewGeneration();
        window.ShowTransforming((string)FindResource("TransformingMessage"));
        _ = TransformAsync(session, window, inputSnapshot, action, instructionsSnapshot, generation);
    }

    private static TextTransformationSettings CreateTransformationSettings(ModelProfile profile, ConnectionDefinition connection)
    {
        int contextSize = profile.ProviderOptions.TryGetValue("num_ctx", out object? value) && int.TryParse(value?.ToString(), out int parsed) ? parsed : 8192;
        ThinkingMode thinking = profile.ProviderOptions.TryGetValue("think", out object? think) && Enum.TryParse(think?.ToString(), true, out ThinkingMode parsedThinking) ? parsedThinking : ThinkingMode.Off;
        return new TextTransformationSettings(connection.Endpoint, profile.Model, profile.Temperature, profile.Timeout, contextSize, thinking);
    }

    private async Task OfferProviderFailureDowngradeAsync(InvocationSession session, MainWindow window, string inputText, ActionDefinition action, string? supplementaryInstructions, ProfileResolver resolver, ProfileResolution usedResolution, string providerFailure, int generation)
    {
        if (usedResolution.Connection is null)
        {
            window.ShowFailure(providerFailure);
            return;
        }

        ProfileResolution downgrade = resolver.OfferDowngradeAfterProviderFailure(usedResolution.Connection.Category, providerFailure);
        if (downgrade.Kind == ProfileResolutionKind.UserConfirmationRequired && ConfirmDowngrade(window, downgrade.Status))
        {
            await TransformWithResolutionAsync(session, window, inputText, action, supplementaryInstructions, resolver, downgrade, generation);
            return;
        }

        window.ShowFailure(downgrade.Kind == ProfileResolutionKind.Failed ? downgrade.Status : providerFailure);
    }

    private static bool ConfirmDowngrade(Window owner, string message) =>
        MessageBox.Show(owner, message, (string)Current.FindResource("ProductName"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    /// <summary>Opens the optional per-invocation instructions editor without changing action data.</summary>
    private void EditInstructions(MainWindow window, InvocationSession session) => TryCollectInstructions(window, session);

    /// <summary>Prepares the existing window for a separate manual transformation.</summary>
    private static void ResetSession(MainWindow window, InvocationSession session) => window.ResetForNewInput();

    /// <summary>Runs one isolated user message without an action prompt or supplementary instructions.</summary>
    private void StartFreeTransformation(MainWindow window, InvocationSession session)
    {
        var dialog = new FreeWindow { Owner = window };
        if (dialog.ShowDialog() != true) return;
        window.SetFreeInput(dialog.Input);
        StartTransformation(window, session, isFreeInvocation: true);
    }

    private static bool TryCollectInstructions(MainWindow owner, InvocationSession session, string? question = null)
    {
        var dialog = new InstructionsWindow(session.SupplementaryInstructions, question) { Owner = owner };
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
        var actions = new MenuItem { Header = Label("ActionsLabel"), Style = itemStyle };
        actions.Click += (_, _) => ShowActions();
        var translationReview = new MenuItem { Header = Label("TranslationReviewLabel"), Style = itemStyle };
        translationReview.Click += (_, _) => ShowTranslationReview();
        var open = new MenuItem { Header = Label("OpenLabel"), Style = itemStyle };
        open.Click += (_, _) => _ = CaptureWithFeedbackAsync(0, hasCopiedText: false);
        var about = new MenuItem { Header = Label("AboutLabel"), Style = itemStyle };
        about.Click += (_, _) => ShowAbout();
        var exit = new MenuItem { Header = Label("ExitLabel"), Style = itemStyle };
        exit.Click += (_, _) => Shutdown();
        menu.Items.Add(toggle);
        menu.Items.Add(new Separator { Style = separatorStyle });
        menu.Items.Add(open);
        menu.Items.Add(actions);
        menu.Items.Add(translationReview);
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
            settingsWindow.ActivateForUserInput();
            return;
        }

        if (startupNoticeWindow is not null || aboutWindow is not null) return;

        settingsWindow = new SettingsWindow();
        settingsWindow.FullLogActivityChanged += active => sessionWindow?.SetFullLogActive(active);
        settingsWindow.UiTranslationGenerated += (_, args) => ApplyCachedLocale(args.Language, args.PreferEnglishUi);
        settingsWindow.SettingsSaved += (_, _) =>
        {
            ConfigureDebugLog();
            ConfigureKeyboardHook(UserConfiguration.LoadConfiguration());
            ApplyCachedLocale(UserConfiguration.LoadConfiguration().UserLanguage, UserConfiguration.LoadLocalizationPreferences().PreferEnglishUi);
            if (localeFallbackNotice is not null) tray?.ShowInfo(localeFallbackNotice);
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
            aboutWindow.ActivateForUserInput();
            return;
        }

        if (settingsWindow is not null || startupNoticeWindow is not null) return;

        aboutWindow = new AboutWindow();
        aboutWindow.Closed += (_, _) => aboutWindow = null;
        aboutWindow.ShowDialog();
    }

    private void ShowActions()
    {
        if (actionsWindow is { IsVisible: true }) { actionsWindow.ActivateForUserInput(); return; }
        void ReloadSessionActions()
        {
            if (sessionWindow is null) return;
            ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
            sessionWindow.ReloadActions(new ActionLoader(configuration.Profiles.Select(profile => profile.Id), actionsDirectory).Load());
        }

        actionsWindow = new ActionsWindow();
        actionsWindow.ActionsChanged += (_, _) => ReloadSessionActions();
        actionsWindow.Closed += (_, _) =>
        {
            ReloadSessionActions();
            actionsWindow = null;
        };
        actionsWindow.Show();
        actionsWindow.ActivateForUserInput();
    }

    /// <summary>Opens the local correction editor for the configured UI language.</summary>
    private void ShowTranslationReview()
    {
        if (translationReviewWindow is { IsVisible: true }) { translationReviewWindow.Activate(); return; }
        string language = UserConfiguration.LoadConfiguration().UserLanguage;
        translationReviewWindow = new TranslationReviewWindow(language);
        translationReviewWindow.TranslationsChanged += (_, _) =>
        {
            ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
            ApplyCachedLocale(configuration.UserLanguage, UserConfiguration.LoadLocalizationPreferences().PreferEnglishUi);
        };
        translationReviewWindow.Closed += (_, _) => translationReviewWindow = null;
        translationReviewWindow.Show();
        translationReviewWindow.Activate();
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

    /// <summary>Replaces the global hook only after a complete valid shortcut configuration is saved.</summary>
    private void ConfigureKeyboardHook(ConfigurationSnapshot configuration)
    {
        KeyboardHook? previous = hook;
        var replacement = new KeyboardHook(configuration.NormalShortcut, configuration.TranslationShortcut);
        replacement.Triggered += (source, hasCopiedText, shortcut) => Dispatcher.BeginInvoke(new Action(() => _ = CaptureWithFeedbackAsync(source, hasCopiedText, shortcut)));
        hook = replacement;
        previous?.Dispose();
    }

    /// <summary>Applies only a fully validated cached locale; English remains active for every failure path.</summary>
    private void ApplyCachedLocale(string language, bool preferEnglishUi)
    {
        var catalog = new LocalizationCatalog(EnglishStringCatalog.Values);
        string cachePath = Path.Combine(UserConfiguration.GetUserDataDirectory(), "locales", language + ".json");
        bool useEnglish = preferEnglishUi || language.Equals("en", StringComparison.OrdinalIgnoreCase);
        LocaleCatalogLoadResult result = useEnglish
            ? new LocaleCatalogLoadResult(EnglishStringCatalog.Values, LocaleCatalogLoadStatus.Loaded)
            : catalog.Load(cachePath);
        isUiTranslationRequired = !preferEnglishUi
            && !language.Equals("en", StringComparison.OrdinalIgnoreCase)
            && result.Status != LocaleCatalogLoadStatus.Loaded;
        string localesDirectory = Path.Combine(UserConfiguration.GetUserDataDirectory(), "locales");
        IReadOnlyDictionary<string, string> values = !useEnglish && result.Status == LocaleCatalogLoadStatus.Loaded
            ? new LanguagePackService(catalog).ApplyOverrides(result.Values, LanguagePackService.GetOverridesPath(localesDirectory, language))
            : result.Values;
        foreach ((string key, string value) in values) Resources[key] = value;
        BuiltInActionCatalog.DisplayNameResolver = id => UiStrings.TryGet($"Action.{id}");
        localeFallbackNotice = result.Status switch
        {
            LocaleCatalogLoadStatus.Stale => (string)FindResource("LocaleCacheStaleFallbackMessage"),
            LocaleCatalogLoadStatus.Invalid => (string)FindResource("LocaleCacheInvalidFallbackMessage"),
            LocaleCatalogLoadStatus.Unreadable => (string)FindResource("LocaleCacheUnreadableFallbackMessage"),
            _ => null
        };
        sessionWindow?.RefreshLocalizedActionLabels();
        sessionWindow?.SetUiTranslationRequired(isUiTranslationRequired);
        actionsWindow?.RefreshLocalizedLabels();
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
