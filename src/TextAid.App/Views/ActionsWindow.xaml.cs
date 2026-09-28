using System.Windows;
using System.Windows.Interop;
using TextAid.Platform.Windows;
using TextAid.App.ViewModels;
using TextAid.App.Localization;
namespace TextAid.App.Views;
public partial class ActionsWindow : Window
{
    public event EventHandler? ActionsChanged;
    public ActionsWindow()
    {
        InitializeComponent();
        var viewModel = new ActionsViewModel();
        viewModel.ActionsSaved += (_, _) => ActionsChanged?.Invoke(this, EventArgs.Empty);
        viewModel.DeleteConfirmationRequested += label => MessageBox.Show(this, string.Format(UiStrings.Get("DeleteActionConfirmation"), label), UiStrings.Get("ProductName"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        DataContext = viewModel;
        SourceInitialized += (_, _) => WindowsShell.TryEnableDarkCaption(new WindowInteropHelper(this).Handle);
    }

    /// <summary>Refreshes action names after the active UI locale changes.</summary>
    public void RefreshLocalizedLabels() => ((ActionsViewModel)DataContext).RefreshLocalizedLabels();
}
