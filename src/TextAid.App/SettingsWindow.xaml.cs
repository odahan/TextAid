using System.Windows;
using System.Windows.Interop;
using TextAid.Platform.Windows;

namespace TextAid.App;

/// <summary>Hosts the V0.1 settings entry point.</summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowsShell.TryEnableDarkCaption(new WindowInteropHelper(this).Handle);
    }
}
