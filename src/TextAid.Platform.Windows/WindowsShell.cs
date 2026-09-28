using System.Runtime.InteropServices;

namespace TextAid.Platform.Windows;

/// <summary>Provides source-monitor geometry and native dark window chrome.</summary>
public static class WindowsShell
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBeforeWindows10 = 19;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const int DarkCaptionColor = 0x261D17; // #171D26 in COLORREF BGR order.
    private const int DarkBorderColor = 0x59483B; // #3B4859 in COLORREF BGR order.
    private const int LightTextColor = 0xFAF6F3; // #F3F6FA in COLORREF BGR order.
    public static bool CenterWindowOnSource(nint window, nint sourceWindow, double width, double height)
    {
        nint monitor = MonitorFromWindow(sourceWindow, 2);
        MONITORINFO info = new() { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (monitor == 0 || !GetMonitorInfo(monitor, ref info)) return false;
        uint dpi = GetDpiForWindow(sourceWindow);
        double scale = dpi == 0 ? 1 : dpi / 96.0;
        int pixelWidth = (int)Math.Round(width * scale);
        int pixelHeight = (int)Math.Round(height * scale);
        int left = info.work.left + (info.work.right - info.work.left - pixelWidth) / 2;
        int top = info.work.top + (info.work.bottom - info.work.top - pixelHeight) / 2;
        return SetWindowPos(window, 0, left, top, pixelWidth, pixelHeight, 0x0004 | 0x0010);
    }

    /// <summary>Applies the TextAid dark title bar, border, and title text to a native WPF window.</summary>
    public static bool TryEnableDarkCaption(nint window)
    {
        int enabled = 1;
        bool immersiveDarkModeApplied = DwmSetWindowAttribute(window, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int)) == 0 ||
                                        DwmSetWindowAttribute(window, DwmwaUseImmersiveDarkModeBeforeWindows10, ref enabled, sizeof(int)) == 0;

        int captionColor = DarkCaptionColor;
        int borderColor = DarkBorderColor;
        int textColor = LightTextColor;
        bool colorsApplied = DwmSetWindowAttribute(window, DwmwaCaptionColor, ref captionColor, sizeof(int)) == 0;
        colorsApplied |= DwmSetWindowAttribute(window, DwmwaBorderColor, ref borderColor, sizeof(int)) == 0;
        colorsApplied |= DwmSetWindowAttribute(window, DwmwaTextColor, ref textColor, sizeof(int)) == 0;
        return immersiveDarkModeApplied || colorsApplied;
    }

    /// <summary>Brings a TextAid window to the foreground after an explicit user invocation from the notification icon.</summary>
    public static bool TryActivateWindow(nint window)
    {
        nint foregroundWindow = GetForegroundWindow();
        uint foregroundThread = foregroundWindow == 0 ? 0 : GetWindowThreadProcessId(foregroundWindow, out _);
        uint currentThread = GetCurrentThreadId();
        bool attached = foregroundThread != 0 && foregroundThread != currentThread && AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            BringWindowToTop(window);
            SetForegroundWindow(window);
            SetFocus(window);
            return GetForegroundWindow() == window;
        }
        finally
        {
            if (attached) AttachThreadInput(currentThread, foregroundThread, false);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int left, top, right, bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO { public int cbSize; public RECT monitor, work; public uint flags; }
    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MONITORINFO info);
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(nint window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")]
    private static extern nint SetFocus(nint window);
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool attach);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
}
