using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Navigation;
using TextAid.Platform.Windows;

namespace TextAid.App.Views;

/// <summary>Shows product identity and the version embedded in the application assembly.</summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        Assembly assembly = Assembly.GetExecutingAssembly();
        string? informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        string? version = informationalVersion?.Split('+', 2)[0] ?? assembly.GetName().Version?.ToString(3);
        DataContext = new { VersionText = $"Version {version}" };
        SourceInitialized += (_, _) => WindowsShell.TryEnableDarkCaption(new WindowInteropHelper(this).Handle);
    }

    private void OnNavigate(object sender, RequestNavigateEventArgs args)
    {
        Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri) { UseShellExecute = true });
        args.Handled = true;
    }
}
