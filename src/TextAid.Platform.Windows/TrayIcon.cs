using System.Drawing;
using System.Windows.Forms;

namespace TextAid.Platform.Windows;

/// <summary>Owns the Windows notification icon without exposing Win32 details to views.</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon icon;

    public TrayIcon(Stream imageStream)
    {
        icon = new NotifyIcon { Icon = new Icon(imageStream), Text = "TextAid", Visible = true };
        icon.MouseUp += (_, args) => { if (args.Button is MouseButtons.Left or MouseButtons.Right) Clicked?.Invoke(); };
    }

    public event Action? Clicked;

    public void ShowError(string message) => icon.ShowBalloonTip(4000, "TextAid", message, ToolTipIcon.Error);

    public void Dispose()
    {
        icon.Visible = false;
        icon.Dispose();
    }
}
