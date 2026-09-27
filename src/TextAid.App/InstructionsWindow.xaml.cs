using System.Windows;

namespace TextAid.App;

/// <summary>Collects non-persistent user guidance for one transformation invocation.</summary>
public partial class InstructionsWindow : Window
{
    /// <summary>Initializes the dialog with any instructions already entered for this session.</summary>
    public InstructionsWindow(string? instructions)
    {
        InitializeComponent();
        InstructionsEditor.Text = instructions ?? string.Empty;
        Loaded += (_, _) => InstructionsEditor.Focus();
    }

    /// <summary>Gets the instructions confirmed by the user.</summary>
    public string Instructions => InstructionsEditor.Text.Trim();

    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
