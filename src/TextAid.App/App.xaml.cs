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
        actionsDirectory = UserConfiguration.EnsureActionsDirectory();
        new ActionLoader(actionsDirectory: actionsDirectory).Load();
        hook = new KeyboardHook();
        hook.Triggered += (source, hasCopiedText) => Dispatcher.BeginInvoke(new Action(() => _ = CaptureWithFeedbackAsync(source, hasCopiedText)));
        using Stream icon = GetResourceStream(new Uri("pack://application:,,,/TextAid;component/Assets/TextAid.ico")).Stream;
        tray = new TrayIcon(icon);
        tray.Clicked += () => Dispatcher.BeginInvoke(ShowTrayMenu);
        Dispatcher.BeginInvoke(new Action(() => _ = CheckInitialProviderAsync()));
    }

    private async Task CheckInitialProviderAsync()
    {
        try
        {
            TextTransformationSettings settings = UserConfiguration.LoadTransformationSettings();
            var factory = new OllamaChatClientFactory();
            bool running = await factory.TestConnectionAsync(settings.Endpoint, CancellationToken.None);
            if (!running || (await factory.GetLocalModelNamesAsync(settings.Endpoint, CancellationToken.None)).Count == 0) ShowStartupNotice();
        }
        catch
        {
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
        catch (Exception)
        {
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
            if (hasCopiedText)
            {
                try { text = await ClipboardReader.ReadUnicodeTextAsync(CancellationToken.None); }
                catch (InvalidOperationException) { text = string.Empty; failed = true; }
            }
            else text = string.Empty;
            bool noText = !failed && string.IsNullOrWhiteSpace(text);
            nint safeSource = noText ? 0 : source;
            var session = new InvocationSession(safeSource, text ?? string.Empty)
            {
                State = failed ? InvocationState.Failed : InvocationState.Ready
            };
            string key = failed ? "ClipboardError" : noText ? "EnterTextMessage" : "ReadyToProcessMessage";
            IReadOnlyList<ActionDefinition> actions = new ActionLoader(actionsDirectory: actionsDirectory).Load();
            sessionWindow = new MainWindow(session, actions, ReplaceOutput, CopyResult, StartTransformation, EditInstructions, ResetSession, (string)FindResource(key));
            sessionWindow.Closed += (_, _) => sessionWindow = null;
            sessionWindow.Show();
            sessionWindow.Activate();
        }
        finally { capturing = false; }
    }

    private async Task TransformAsync(InvocationSession session, MainWindow window, string inputText, ActionDefinition action, string? supplementaryInstructions)
    {
        try
        {
            TextTransformationSettings settings = UserConfiguration.LoadTransformationSettings();
            if (!settings.HasModel)
            {
                window.ShowFailure((string)FindResource("NoModelError"));
                return;
            }

            if (action.TemperatureOverride is not null) settings = settings with { Temperature = action.TemperatureOverride.Value };
            string instruction = TemplateRenderer.Render(action.PromptTemplate, inputText);
            if (!string.IsNullOrWhiteSpace(supplementaryInstructions))
                instruction = $"{instruction}\n\nAdditional user instructions for this invocation:\n{supplementaryInstructions}";
            string output = await transformationService.TransformAsync(
                new TextTransformationRequest(
                    inputText,
                    instruction,
                    settings),
                session.Cancellation.Token);
            if (!window.IsLoaded) return;
            window.ShowResult(output);
        }
        catch (OperationCanceledException) when (session.Cancellation.IsCancellationRequested)
        {
            // Closing the session is a normal cancellation path.
        }
        catch (OperationCanceledException)
        {
            window.ShowFailure((string)FindResource("TransformationTimeoutError"));
        }
        catch (Exception)
        {
            window.ShowFailure((string)FindResource("TransformationError"));
        }
    }

    /// <summary>Starts a user-requested transformation from the editable session input.</summary>
    private void StartTransformation(MainWindow window, InvocationSession session)
    {
        if (string.IsNullOrWhiteSpace(session.InputText) || session.State == InvocationState.Transforming) return;
        ActionDefinition? action = new ActionLoader(actionsDirectory: actionsDirectory).Load().FirstOrDefault(candidate => candidate.Id.Equals(session.ActionId, StringComparison.OrdinalIgnoreCase));
        if (action is null)
        {
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
        settingsWindow.Closed += (_, _) => settingsWindow = null;
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
        hook?.Dispose();
        tray?.Dispose();
        instanceMutex?.ReleaseMutex();
        instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
