using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TextAid.Platform.Windows;

/// <summary>Observes configured keyboard sequences and consumes their final key so source apps do not receive it.</summary>
public sealed class KeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private readonly ShortcutSequenceDetector normalDetector;
    private readonly ShortcutSequenceDetector translationDetector;
    private readonly HashSet<int> activeModifiers = [];
    private readonly HashSet<int> suppressedKeys = [];
    private readonly HookProcedure callback;
    private nint handle;
    private uint clipboardSequenceBeforeCopy;

    public KeyboardHook(string normalShortcut = "Ctrl+C+C", string translationShortcut = "Ctrl+C+T")
    {
        normalDetector = new ShortcutSequenceDetector(normalShortcut, TimeSpan.FromSeconds(1));
        translationDetector = new ShortcutSequenceDetector(translationShortcut, TimeSpan.FromSeconds(1));
        callback = OnKeyboard;
        using Process process = Process.GetCurrentProcess();
        using ProcessModule? module = process.MainModule;
        handle = SetWindowsHookEx(WhKeyboardLl, callback, GetModuleHandle(module?.ModuleName), 0);
        if (handle == 0) throw new System.ComponentModel.Win32Exception();
    }

    /// <summary>Raised after a double-copy gesture and indicates whether its first copy changed the clipboard.</summary>
    public event Action<nint, bool, InvocationShortcut>? Triggered;

    private nint OnKeyboard(int code, nint message, nint data)
    {
        if (code >= 0)
        {
            int key = Marshal.ReadInt32(data);
            if (message == WmKeyDown || message == WmSysKeyDown)
            {
                if (IsModifier(key)) activeModifiers.Add(NormalizeModifier(key));
                DateTimeOffset now = DateTimeOffset.UtcNow;
                bool normalTriggered = normalDetector.KeyDown(key, now, activeModifiers);
                bool translationTriggered = translationDetector.KeyDown(key, now, activeModifiers);
                if (key == 0x43 && !normalTriggered && !translationTriggered) clipboardSequenceBeforeCopy = GetClipboardSequenceNumber();
                if (normalTriggered || translationTriggered)
                {
                    nint source = GetForegroundWindow();
                    Triggered?.Invoke(source, GetClipboardSequenceNumber() != clipboardSequenceBeforeCopy, normalTriggered ? InvocationShortcut.Choose : InvocationShortcut.Translate);
                    suppressedKeys.Add(key);
                    return 1;
                }
            }
            else if (message == WmKeyUp || message == WmSysKeyUp)
            {
                normalDetector.KeyUp(key);
                translationDetector.KeyUp(key);
                if (IsModifier(key)) activeModifiers.Remove(NormalizeModifier(key));
                if (suppressedKeys.Remove(key)) return 1;
            }
        }
        return CallNextHookEx(handle, code, message, data);
    }

    public void Dispose()
    {
        if (handle != 0) { UnhookWindowsHookEx(handle); handle = 0; }
        GC.SuppressFinalize(this);
    }

    private delegate nint HookProcedure(int code, nint message, nint data);

    private static bool IsModifier(int key) => key is 0x10 or 0xA0 or 0xA1 or 0x11 or 0xA2 or 0xA3 or 0x12 or 0xA4 or 0xA5;
    private static int NormalizeModifier(int key) => key is 0xA0 or 0xA1 ? 0x10 : key is 0xA2 or 0xA3 ? 0x11 : key is 0xA4 or 0xA5 ? 0x12 : key;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HookProcedure procedure, nint module, uint threadId);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hook, int code, nint message, nint data);
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? moduleName);
}

/// <summary>Identifies the configured invocation route that opened a session.</summary>
public enum InvocationShortcut { Choose, Translate }
