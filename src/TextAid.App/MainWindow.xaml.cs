using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TextAid.Core;
using TextAid.Platform.Windows;

namespace TextAid.App;

/// <summary>Hosts one short-lived invocation and places it on the source monitor.</summary>
public partial class MainWindow : Window
{
    public MainWindow(InvocationSession session, string status)
    {
        InitializeComponent();
        DataContext = new MainViewModel(session, Close, status);

        SourceInitialized += (_, _) =>
        {
            nint handle = new WindowInteropHelper(this).Handle;
            WindowsShell.CenterWindowOnSource(handle, session.SourceWindow, Width, Height);
            WindowsShell.TryEnableDarkCaption(handle);
        };
        Loaded += (_, _) => WindowsShell.CenterWindowOnSource(
            new WindowInteropHelper(this).Handle, session.SourceWindow, Width, Height);
        PreviewKeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape) Close();
        };
        Closed += (_, _) =>
        {
            if (session.State != InvocationState.Accepted) session.State = InvocationState.Cancelled;
            session.Cancellation.Cancel();
            session.Dispose();
        };
    }
}
