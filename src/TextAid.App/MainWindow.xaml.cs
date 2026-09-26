using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TextAid.Core;
using TextAid.Platform.Windows;

namespace TextAid.App;

/// <summary>Hosts one short-lived invocation and places it on the source monitor.</summary>
public partial class MainWindow : Window
{
    public MainWindow(InvocationSession session, Action<MainWindow, InvocationSession> replace, Action<MainWindow, InvocationSession> copy, Action<MainWindow, InvocationSession> process, string status)
    {
        InitializeComponent();
        DataContext = new MainViewModel(session, () => replace(this, session), () => copy(this, session), () => process(this, session), Close, status);

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
}
