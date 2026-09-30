using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.ComponentModel;
using TextAid.Platform.Windows;
using TextAid.App.ViewModels;

namespace TextAid.App.Views;

/// <summary>Hosts local Ollama settings and model discovery.</summary>
public partial class SettingsWindow : Window
{
    /// <summary>Raised after the user saves settings successfully.</summary>
    public event EventHandler? SettingsSaved;
    /// <summary>Raised whenever the effective Full log selection changes in the Settings editor.</summary>
    public event Action<bool>? FullLogActivityChanged;
    /// <summary>Raised after a valid UI translation catalog has been generated.</summary>
    public event EventHandler<UiTranslationGeneratedEventArgs>? UiTranslationGenerated;

    public SettingsWindow()
    {
        InitializeComponent();
        var viewModel = new SettingsViewModel();
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.UiTranslationGenerated += (_, args) => UiTranslationGenerated?.Invoke(this, args);
        viewModel.Saved += (_, _) =>
        {
            SettingsSaved?.Invoke(this, EventArgs.Empty);
            Close();
        };
        Loaded += async (_, _) => await viewModel.InitializeAsync();
        Loaded += (_, _) => ActivateForUserInput();
        SourceInitialized += (_, _) => WindowsShell.TryEnableDarkCaption(new WindowInteropHelper(this).Handle);
    }

    /// <summary>Activates the settings window after an explicit tray-menu request.</summary>
    public void ActivateForUserInput()
    {
        Activate();
        WindowsShell.TryActivateWindow(new WindowInteropHelper(this).Handle);
        Activate();
        Focus();
    }

    private void OnNetworkSecretChanged(object sender, RoutedEventArgs e) => ((SettingsViewModel)DataContext).NetworkSecret = ((PasswordBox)sender).Password;

    private void OnExternalSecretChanged(object sender, RoutedEventArgs e) => ((SettingsViewModel)DataContext).ExternalSecret = ((PasswordBox)sender).Password;

    private void OnExternalModelSelected(object sender, SelectionChangedEventArgs e)
    {
        if (((ComboBox)sender).SelectedItem is string model) ((SettingsViewModel)DataContext).ExternalModel = model;
    }

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
