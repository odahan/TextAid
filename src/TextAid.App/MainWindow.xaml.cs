using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using TextAid.Core;
using TextAid.Platform.Windows;

namespace TextAid.App;

/// <summary>Hosts one short-lived invocation and places it on the source monitor.</summary>
public partial class MainWindow : Window
{
    private readonly Action showApplicationMenu;

    public MainWindow(InvocationSession session, IReadOnlyList<ActionDefinition> actions, IReadOnlyList<string> presetActionIds, Action showApplicationMenu, Action<MainWindow, InvocationSession> replace, Action<MainWindow, InvocationSession> copy, Action<MainWindow, InvocationSession> process, Action<MainWindow, InvocationSession> instructions, Action<MainWindow, InvocationSession> reset, string status, bool isFullLogActive)
    {
        InitializeComponent();
        this.showApplicationMenu = showApplicationMenu ?? throw new ArgumentNullException(nameof(showApplicationMenu));
        DataContext = new MainViewModel(session, actions, presetActionIds, () => replace(this, session), () => copy(this, session), () => process(this, session), () => instructions(this, session), () => reset(this, session), Close, status, isFullLogActive);

        SourceInitialized += (_, _) =>
        {
            nint handle = new WindowInteropHelper(this).Handle;
            WindowsShell.CenterWindowOnSource(handle, session.SourceWindow, Width, Height);
            WindowsShell.TryEnableDarkCaption(handle);
        };
        Loaded += (_, _) =>
        {
            WindowsShell.CenterWindowOnSource(new WindowInteropHelper(this).Handle, session.SourceWindow, Width, Height);
            Topmost = true;
            Activate();
            Focus();
            InputEditor.Focus();
            Topmost = false;
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
    public void ShowResult(string output) => ((MainViewModel)DataContext).ShowResult(output);

    /// <summary>Refreshes the view when processing starts from the editable input.</summary>
    public void ShowTransforming(string message) => ((MainViewModel)DataContext).ShowTransforming(message);

    /// <summary>Clears this transaction for a safe new manual input.</summary>
    public void ResetForNewInput() => ((MainViewModel)DataContext).ResetForNewInput();

    /// <summary>Updates the persistent Full log indicator without closing the active session.</summary>
    public void SetFullLogActive(bool active) => ((MainViewModel)DataContext).SetFullLogActive(active);

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
}
