using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using TextAid.App;
using TextAid.Core;
using TextAid.Platform.Windows;
using TextAid.App.ViewModels;

namespace TextAid.App.Views;

/// <summary>Hosts one short-lived invocation and places it on the source monitor.</summary>
public partial class MainWindow : Window
{
    private readonly Action showApplicationMenu;
    private readonly InvocationSession session;

    public MainWindow(InvocationSession session, IReadOnlyList<ActionDefinition> actions, IReadOnlyList<string> presetActionIds, Action showApplicationMenu, Action<MainWindow, InvocationSession> replace, Action<MainWindow, InvocationSession> copy, Action<MainWindow, InvocationSession> process, Action<MainWindow, InvocationSession> instructions, Action<MainWindow, InvocationSession> reset, string status, bool isFullLogActive, bool isUiTranslationRequired)
    {
        InitializeComponent();
        this.session = session;
        this.showApplicationMenu = showApplicationMenu ?? throw new ArgumentNullException(nameof(showApplicationMenu));
        var viewModel = new MainViewModel(session, actions, presetActionIds, () => replace(this, session), () => copy(this, session), () => process(this, session), () => instructions(this, session), () => reset(this, session), Close, status, isFullLogActive, isUiTranslationRequired);
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        DataContext = viewModel;

        SourceInitialized += (_, _) =>
        {
            nint handle = new WindowInteropHelper(this).Handle;
            WindowsShell.CenterWindowOnSource(handle, session.SourceWindow, Width, Height);
            WindowsShell.TryEnableDarkCaption(handle);
        };
        Loaded += (_, _) =>
        {
            WindowsShell.CenterWindowOnSource(new WindowInteropHelper(this).Handle, session.SourceWindow, Width, Height);
            ActivateForUserInput();
        };
        PreviewKeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape) Close();
        };
        Closed += (_, _) =>
        {
            if (session.State is not InvocationState.Replaced and not InvocationState.Copied) session.State = InvocationState.Cancelled;
            session.Cancellation.Cancel();
            session.Dispose();
        };
    }

    /// <summary>Shows a safe operation failure and leaves only cancellation available.</summary>
    public void ShowFailure(string message) => ((MainViewModel)DataContext).ShowFailure(message);

    /// <summary>Shows the completed transformation and enables the safe result actions.</summary>
    public void ShowResult(string output)
    {
        ((MainViewModel)DataContext).ShowResult(output);
        ShowRenderedMarkdownIfAvailable();
    }

    /// <summary>Refreshes the view when processing starts from the editable input.</summary>
    public void ShowTransforming(string message) => ((MainViewModel)DataContext).ShowTransforming(message);

    /// <summary>Clears this transaction for a safe new manual input.</summary>
    public void ResetForNewInput()
    {
        MarkdownPreview.Document = null;
        MarkdownPreview.Visibility = Visibility.Collapsed;
        PlainTextPreview.Visibility = Visibility.Visible;
        ((MainViewModel)DataContext).ResetForNewInput();
    }

    /// <summary>Updates the persistent Full log indicator without closing the active session.</summary>
    public void SetFullLogActive(bool active) => ((MainViewModel)DataContext).SetFullLogActive(active);

    /// <summary>Updates the persistent UI-translation warning without closing the active session.</summary>
    public void SetUiTranslationRequired(bool required) => ((MainViewModel)DataContext).SetUiTranslationRequired(required);

    /// <summary>Shows the deployment category of the provider selected for this invocation.</summary>
    public void SetActiveConnection(ConnectionCategory category) => ((MainViewModel)DataContext).SetActiveConnection(category);

    /// <summary>Refreshes the action selector after a validated editor save.</summary>
    public void ReloadActions(IReadOnlyList<ActionDefinition> actions) => ((MainViewModel)DataContext).ReloadActions(actions);

    /// <summary>Refreshes action captions after the active UI locale changes.</summary>
    public void RefreshLocalizedActionLabels() => ((MainViewModel)DataContext).RefreshLocalizedActionLabels();

    /// <summary>Activates this invocation window before placing keyboard input in its editor.</summary>
    public void ActivateForUserInput()
    {
        Activate();
        WindowsShell.TryActivateWindow(new WindowInteropHelper(this).Handle);
        Activate();
        InputEditor.Focus();
        Keyboard.Focus(InputEditor);
    }

    private void OnEditPreset(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out int slot)) return;
        var menu = new ContextMenu { Style = (Style)FindResource("TrayContextMenuStyle") };
        MainViewModel viewModel = (MainViewModel)DataContext;
        foreach (ActionDefinition action in viewModel.Actions)
        {
            var item = new MenuItem { Header = action.DisplayName, Style = (Style)FindResource("TrayMenuItemStyle") };
            item.Click += (_, _) =>
            {
                UserConfiguration.SaveActionPreset(slot, action.Id);
                viewModel.AssignPreset(slot, action);
            };
            menu.Items.Add(item);
        }

        menu.PlacementTarget = button;
        menu.Placement = PlacementMode.Bottom;
        button.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private void OnShowApplicationMenu(object sender, RoutedEventArgs e) => showApplicationMenu();

    private void OnShowRawMarkdown(object sender, RoutedEventArgs e)
    {
        MarkdownPreview.Visibility = Visibility.Collapsed;
        PlainTextPreview.Visibility = Visibility.Visible;
    }

    private void OnShowRenderedMarkdown(object sender, RoutedEventArgs e) => ShowRenderedMarkdownIfAvailable();

    private void ShowRenderedMarkdownIfAvailable()
    {
        FlowDocument? document = null;
        bool rendered = session.MarkdownOutputEnabled
            && session.HasCurrentResult
            && MarkdownPreviewRenderer.TryCreateDocument(session.OutputText!, out document);

        MarkdownPreview.Document = rendered ? document : null;
        MarkdownPreview.Visibility = rendered ? Visibility.Visible : Visibility.Collapsed;
        PlainTextPreview.Visibility = rendered ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.MarkdownOutputEnabled)
            && sender is MainViewModel { MarkdownOutputEnabled: false })
        {
            OnShowRawMarkdown(this, new RoutedEventArgs());
        }
    }
}
