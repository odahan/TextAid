using System.Windows;
using System.Windows.Documents;

namespace TextAid.TestTarget;

/// <summary>Provides text controls and visible focus events for safe-replacement testing.</summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RichText.Document.Blocks.Add(new Paragraph(new Run("RichTextBox content supports selection and paste checks.")));
        GotKeyboardFocus += (_, eventArgs) => Events.Text = $"Focused: {eventArgs.NewFocus?.GetType().Name}";
    }

    private void MoveFocus_Click(object sender, RoutedEventArgs eventArgs)
    {
        Events.Text = "Focus intentionally moved away from editable text.";
    }
}
