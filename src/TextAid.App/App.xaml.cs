using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TextAid.Core;
using TextAid.Platform.Windows;

namespace TextAid.App;

/// <summary>Owns the resident tray lifecycle and one active invocation session.</summary>
public partial class App : Application
{
    private KeyboardHook? hook;
    private TrayIcon? tray;
    private MainWindow? sessionWindow;
    private Mutex? instanceMutex;
    private readonly ResultActions resultActions = new(new Win32ResultActions());
    private bool enabled = true;
    private bool capturing;

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
        hook = new KeyboardHook();
        hook.Triggered += source => Dispatcher.BeginInvoke(new Action(() => _ = CaptureWithFeedbackAsync(source)));
        using Stream icon = GetResourceStream(new Uri("pack://application:,,,/TextAid;component/Assets/TextAid.ico")).Stream;
        tray = new TrayIcon(icon);
        tray.Clicked += () => Dispatcher.BeginInvoke(ShowTrayMenu);
    }

    private async Task CaptureWithFeedbackAsync(nint source)
    {
        try
        {
            await CaptureAsync(source);
        }
        catch (Exception)
        {
            tray?.ShowError((string)FindResource("SessionError"));
        }
    }

    private async Task CaptureAsync(nint source)
    {
        if (!enabled || capturing || sessionWindow is not null) return;
        capturing = true;
        try
        {
            string? text;
            bool failed = false;
            if (source == 0)
            {
                text = string.Empty;
                failed = true;
            }
            else
            {
                try { text = await ClipboardReader.ReadUnicodeTextAsync(CancellationToken.None); }
                catch (InvalidOperationException) { text = string.Empty; failed = true; }
            }
            bool noText = !failed && string.IsNullOrWhiteSpace(text);
            var session = new InvocationSession(source, text ?? string.Empty)
            {
                State = failed || noText ? InvocationState.Failed : InvocationState.Ready
            };
            if (session.State == InvocationState.Ready)
            {
                session.OutputText = session.InputText.ToUpperInvariant();
                session.State = InvocationState.ResultReady;
            }
            string key = source == 0 ? "SourceWindowError" : failed ? "ClipboardError" : noText ? "NoTextError" : "ShellMessage";
            sessionWindow = new MainWindow(session, ReplaceOutput, CopyResult, (string)FindResource(key));
            sessionWindow.Closed += (_, _) => sessionWindow = null;
            sessionWindow.Show();
            sessionWindow.Activate();
        }
        finally { capturing = false; }
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
        settings.Click += (_, _) => new SettingsWindow().Show();
        var about = new MenuItem { Header = Label("AboutLabel"), Style = itemStyle };
        about.Click += (_, _) => new AboutWindow().Show();
        var exit = new MenuItem { Header = Label("ExitLabel"), Style = itemStyle };
        exit.Click += (_, _) => Shutdown();
        menu.Items.Add(toggle);
        menu.Items.Add(new Separator { Style = separatorStyle });
        menu.Items.Add(settings);
        menu.Items.Add(about);
        menu.Items.Add(new Separator { Style = separatorStyle });
        menu.Items.Add(exit);
        menu.IsOpen = true;
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
