using System.Windows;
using System.Windows.Interop;
using TextAid.Platform.Windows;

namespace TextAid.App.Views;

/// <summary>Explains how to configure a provider when no usable local Ollama setup is found.</summary>
public partial class StartupNoticeWindow : Window
{
    public StartupNoticeWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowsShell.TryEnableDarkCaption(new WindowInteropHelper(this).Handle);
    }

    /// <summary>Raised when the user wants to configure a provider.</summary>
    public event EventHandler? SettingsRequested;

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        Close();
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
