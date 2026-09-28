using System.Windows;
using System.Windows.Interop;
using TextAid.Platform.Windows;

namespace TextAid.App.Views;

/// <summary>Collects non-persistent user guidance for one transformation invocation.</summary>
public partial class InstructionsWindow : Window
{
    /// <summary>Initializes the dialog with any instructions already entered for this session.</summary>
    public InstructionsWindow(string? instructions, string? question = null)
    {
        InitializeComponent();
        InstructionsEditor.Text = instructions ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(question)) QuestionText.Text = question;
        Loaded += (_, _) => InstructionsEditor.Focus();
        SourceInitialized += (_, _) => WindowsShell.TryEnableDarkCaption(new WindowInteropHelper(this).Handle);
    }

    /// <summary>Gets the instructions confirmed by the user.</summary>
    public string Instructions => InstructionsEditor.Text.Trim();

    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
