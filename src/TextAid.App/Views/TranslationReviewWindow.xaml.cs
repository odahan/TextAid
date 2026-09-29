using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Win32;
using TextAid.App.Localization;
using TextAid.App.ViewModels;
using TextAid.Platform.Windows;

namespace TextAid.App.Views;

/// <summary>Hosts the local translation correction editor and explicit language-pack exchange commands.</summary>
public partial class TranslationReviewWindow : Window
{
    private readonly TranslationReviewViewModel viewModel;

    /// <summary>Creates the review window for one selected application language.</summary>
    public TranslationReviewWindow(string language)
    {
        InitializeComponent();
        viewModel = new TranslationReviewViewModel(language);
        viewModel.DeleteCacheConfirmationRequested += () => MessageBox.Show(this, UiStrings.Get("DeleteLanguageCacheConfirmation"), UiStrings.Get("ProductName"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        viewModel.LanguageCacheDeleted += (_, _) => Close();
        DataContext = viewModel;
        SourceInitialized += (_, _) => WindowsShell.TryEnableDarkCaption(new WindowInteropHelper(this).Handle);
        Closing += OnClosing;
    }

    /// <summary>Raised when corrections or a compatible active-language import changes UI text.</summary>
    public event EventHandler? TranslationsChanged
    {
        add => viewModel.TranslationsChanged += value;
        remove => viewModel.TranslationsChanged -= value;
    }

    private void OnImport(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "TextAid language pack (*.json)|*.json|All files (*.*)|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) viewModel.Import(dialog.FileName);
    }

    private void OnExport(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "TextAid language pack (*.json)|*.json", FileName = viewModel.Language + ".textaid-language-pack.json", AddExtension = true };
        if (dialog.ShowDialog(this) == true) viewModel.Export(dialog.FileName);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!viewModel.TryPersistPendingCorrections()) e.Cancel = true;
    }

    private void OnTranslationSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TranslationGrid.SelectedItem is not null) TranslationGrid.ScrollIntoView(TranslationGrid.SelectedItem);
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {

    }
}
