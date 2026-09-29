using System.Windows;
using System.Windows.Interop;
using TextAid.Platform.Windows;

namespace TextAid.App.Views;

/// <summary>Collects one isolated message for a Free invocation.</summary>
public partial class FreeWindow : Window
{
    public FreeWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => FreeEditor.Focus();
        SourceInitialized += (_, _) => WindowsShell.TryEnableDarkCaption(new WindowInteropHelper(this).Handle);
    }

    /// <summary>Gets the exact message confirmed by the user without trimming or normalization.</summary>
    public string Input => FreeEditor.Text;

    private void Proceed_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Input)) return;
        DialogResult = true;
    }
}
