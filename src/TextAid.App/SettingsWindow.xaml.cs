using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.ComponentModel;
using TextAid.Platform.Windows;

namespace TextAid.App;

/// <summary>Hosts local Ollama settings and model discovery.</summary>
public partial class SettingsWindow : Window
{
    /// <summary>Raised after the user saves settings successfully.</summary>
    public event EventHandler? SettingsSaved;
    /// <summary>Raised whenever the effective Full log selection changes in the Settings editor.</summary>
    public event Action<bool>? FullLogActivityChanged;

    public SettingsWindow()
    {
        InitializeComponent();
        var viewModel = new SettingsViewModel();
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.Saved += (_, _) =>
        {
            SettingsSaved?.Invoke(this, EventArgs.Empty);
            Close();
        };
        Loaded += async (_, _) => await viewModel.InitializeAsync();
        SourceInitialized += (_, _) => WindowsShell.TryEnableDarkCaption(new WindowInteropHelper(this).Handle);
    }

    private void OnNetworkSecretChanged(object sender, RoutedEventArgs e) => ((SettingsViewModel)DataContext).NetworkSecret = ((PasswordBox)sender).Password;

    private void OnExternalSecretChanged(object sender, RoutedEventArgs e) => ((SettingsViewModel)DataContext).ExternalSecret = ((PasswordBox)sender).Password;

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsViewModel.DebugEnabled) or nameof(SettingsViewModel.FullDebugEnabled))
        {
            var viewModel = (SettingsViewModel)DataContext;
            FullLogActivityChanged?.Invoke(viewModel.IsFullLogActive);
        }
    }
}
