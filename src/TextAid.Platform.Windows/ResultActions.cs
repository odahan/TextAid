using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TextAid.Platform.Windows;

/// <summary>Describes the native operations required to safely paste into a captured window.</summary>
public interface IWindowsResultActions
{
    bool IsWindow(nint window);
    bool RestoreAndFocus(nint window);
    bool IsFocused(nint window);
    bool AreModifiersReleased();
    bool SendPaste(nint window);
}

/// <summary>Represents the outcome of a guarded replacement attempt.</summary>
public enum ReplaceResult
{
    Replaced,
    InvalidTarget,
    FocusFailed,
    ModifiersStillPressed,
    PasteFailed
}

/// <summary>Validates the captured target before emitting a synthetic Ctrl+V paste.</summary>
public sealed class ResultActions(IWindowsResultActions windows)
{
    private static readonly TimeSpan FocusTimeout = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan ModifierTimeout = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(20);

    /// <summary>Attempts to paste only into the window that was captured at invocation.</summary>
    public ReplaceResult TryReplace(nint sourceWindow)
    {
        if (sourceWindow == 0 || !windows.IsWindow(sourceWindow)) return ReplaceResult.InvalidTarget;
        if (!windows.RestoreAndFocus(sourceWindow) || !WaitUntil(() => windows.IsFocused(sourceWindow), FocusTimeout))
            return ReplaceResult.FocusFailed;
        if (!WaitUntil(windows.AreModifiersReleased, ModifierTimeout)) return ReplaceResult.ModifiersStillPressed;
        if (!windows.IsWindow(sourceWindow)) return ReplaceResult.InvalidTarget;
        if (!windows.IsFocused(sourceWindow)) return ReplaceResult.FocusFailed;
        return windows.SendPaste(sourceWindow) ? ReplaceResult.Replaced : ReplaceResult.PasteFailed;
    }

    /// <summary>Records that Copy has no native focus or input side effects.</summary>
    public void Copy()
    {
    }

    private static bool WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        do
        {
            if (condition()) return true;
            Thread.Sleep(PollInterval);
        } while (stopwatch.Elapsed < timeout);
        return false;
    }
}

/// <summary>Uses Win32 to restore, validate, and paste into a captured source window.</summary>
public sealed class Win32ResultActions(Action<ReplacementDiagnostic>? reportDiagnostic = null) : IWindowsResultActions
{
    private const int SwRestore = 9;
    private const ushort KeyControl = 0x11;
    private const ushort KeyV = 0x56;
    private const uint KeyEventKeyUp = 0x0002;
    private static readonly int[] ModifierKeys = [0x11, 0x12, 0x10, 0xA2, 0xA3, 0xA4, 0xA5, 0xA0, 0xA1, 0x5B, 0x5C];

    /// <inheritdoc />
    public bool IsWindow(nint window) => NativeIsWindow(window);

    /// <inheritdoc />
    /// <remarks>Preserves the child-window focus restored by the target application, including WinUI XAML islands.</remarks>
    public bool RestoreAndFocus(nint window)
    {
        Report("replace-before-activation", window);
        if (IsIconic(window)) ShowWindow(window, SwRestore);
        uint sourceThread = GetWindowThreadProcessId(window, out _);
        uint currentThread = GetCurrentThreadId();
        bool attached = sourceThread != 0 && sourceThread != currentThread && AttachThreadInput(currentThread, sourceThread, true);
        try
        {
            BringWindowToTop(window);
            SetForegroundWindow(window);
            Report("replace-after-activation", window);
            return GetForegroundWindow() == window;
        }
        finally
        {
            if (attached) AttachThreadInput(currentThread, sourceThread, false);
        }
    }

    /// <inheritdoc />
    public bool IsFocused(nint window)
    {
        if (GetForegroundWindow() != window) return false;
        nint focusedWindow = ReadFocusedWindow();
        return focusedWindow != 0 && (focusedWindow == window || IsChild(window, focusedWindow));
    }

    /// <inheritdoc />
    public bool AreModifiersReleased() => ModifierKeys.All(key => (GetAsyncKeyState(key) & 0x8000) == 0);

    /// <inheritdoc />
    public bool SendPaste(nint window)
    {
        if (!NativeIsWindow(window) || !IsFocused(window))
        {
            Report("replace-paste-blocked", window);
            return false;
        }

        INPUT[] input =
        [
            KeyboardInput(KeyControl, 0),
            KeyboardInput(KeyV, 0),
            KeyboardInput(KeyV, KeyEventKeyUp),
            KeyboardInput(KeyControl, KeyEventKeyUp)
        ];
        uint sent = SendInput((uint)input.Length, input, Marshal.SizeOf<INPUT>());
        int error = sent == input.Length ? 0 : Marshal.GetLastPInvokeError();
        Report("replace-input-sent", window, sent, error);
        return sent == input.Length;
    }

    /// <summary>Reads focus from the foreground input queue, including XAML island child windows.</summary>
    private static nint ReadFocusedWindow()
    {
        var info = new GUITHREADINFO { cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>() };
        return GetGUIThreadInfo(0, ref info) ? info.hwndFocus : 0;
    }

    /// <summary>Reports native metadata without reading window titles, clipboard data, or user text.</summary>
    private void Report(string stage, nint window, uint inputsSent = 0, int nativeError = 0)
    {
        if (reportDiagnostic is null) return;
        nint foreground = GetForegroundWindow();
        nint focused = ReadFocusedWindow();
        reportDiagnostic(new ReplacementDiagnostic(stage, window, foreground, focused,
            focused != 0 && (focused == window || IsChild(window, focused)), inputsSent, nativeError));
    }

    private static INPUT KeyboardInput(ushort key, uint flags) => new()
    {
        type = 1,
        union = new InputUnion { keyboard = new KEYBDINPUT { wVk = key, dwFlags = flags } }
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion union; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mouse;
        [FieldOffset(0)] public KEYBDINPUT keyboard;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public nint dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public nint dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int left, top, right, bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        public uint cbSize, flags;
        public nint hwndActive, hwndFocus, hwndCapture, hwndMenuOwner, hwndMoveSize, hwndCaret;
        public RECT rcCaret;
    }

    [DllImport("user32.dll", EntryPoint = "IsWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool NativeIsWindow(nint window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint window, int command);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(nint window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(nint window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsChild(nint parent, nint child);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetGUIThreadInfo(uint threadId, ref GUITHREADINFO info);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool attach);
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, INPUT[] input, int size);
}

/// <summary>Contains only native focus and input metadata for an opt-in replacement diagnostic.</summary>
public sealed record ReplacementDiagnostic(string Stage, nint TargetWindow, nint ForegroundWindow,
    nint FocusedWindow, bool FocusWithinTarget, uint InputsSent, int NativeError);
