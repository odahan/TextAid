using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using TextAid.Platform.Windows;

namespace TextAid.App;

/// <summary>Hosts local Ollama settings and model discovery.</summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        var viewModel = new SettingsViewModel();
        DataContext = viewModel;
        viewModel.Saved += (_, _) => Close();
        Loaded += async (_, _) => await viewModel.InitializeAsync();
        SourceInitialized += (_, _) => WindowsShell.TryEnableDarkCaption(new WindowInteropHelper(this).Handle);
    }

    private void OnNetworkSecretChanged(object sender, RoutedEventArgs e) => ((SettingsViewModel)DataContext).NetworkSecret = ((PasswordBox)sender).Password;

    private void OnExternalSecretChanged(object sender, RoutedEventArgs e) => ((SettingsViewModel)DataContext).ExternalSecret = ((PasswordBox)sender).Password;

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
