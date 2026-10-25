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
    private readonly KeyboardModifierState modifierState = new();
    private static readonly int[] ModifierKeys = [0x10, 0xA0, 0xA1, 0x11, 0xA2, 0xA3, 0x12, 0xA4, 0xA5, 0x5B, 0x5C];
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

    /// <summary>Reports targeted shortcut diagnostics; subscribers must defer slow work outside the hook.</summary>
    public event Action<KeyboardShortcutDiagnostic>? Diagnostic;

    private nint OnKeyboard(int code, nint message, nint data)
    {
        if (code >= 0)
        {
            int key = Marshal.ReadInt32(data);
            int scanCode = Marshal.ReadInt32(data, 4);
            int flags = Marshal.ReadInt32(data, 8);
            uint inputTime = unchecked((uint)Marshal.ReadInt32(data, 12));
            bool isDown = message == WmKeyDown || message == WmSysKeyDown;
            bool isUp = message == WmKeyUp || message == WmSysKeyUp;
            if (!isDown && !isUp) return CallNextHookEx(handle, code, message, data);
            if ((flags & 0x12) != 0)
            {
                if (normalDetector.IsPending || translationDetector.IsPending)
                {
                    IReadOnlySet<int> windowsKeys = ReadWindowsModifiers();
                    Diagnostic?.Invoke(new KeyboardShortcutDiagnostic("shortcut-injected-input-rejected", key, scanCode,
                        flags, inputTime, ModifierMask(modifierState.ActiveModifiers),
                        ModifierMask(KeyboardModifierState.NormalizeModifiers(windowsKeys)), 0,
                        windowsKeys.Contains(0xA2), windowsKeys.Contains(0xA3)));
                }
                CancelPendingSequences();
                if (isUp)
                {
                    normalDetector.KeyUp(key);
                    translationDetector.KeyUp(key);
                    if (suppressedKeys.Remove(key)) return 1;
                }
                return CallNextHookEx(handle, code, message, data);
            }

            bool modifier = KeyboardModifierState.IsModifier(key);
            if (modifier)
            {
                modifierState.Update(key, isDown);
                CancelPendingSequences();
            }
            if (message == WmKeyDown || message == WmSysKeyDown)
            {
                if (modifier) return CallNextHookEx(handle, code, message, data);
                IReadOnlySet<int> windowsModifiers = ReadWindowsModifiers();
                var verifiedModifiers = KeyboardModifierState.NormalizeModifiers(windowsModifiers);
                int observedMask = ModifierMask(modifierState.ActiveModifiers);
                if (modifierState.Reconcile(windowsModifiers))
                {
                    CancelPendingSequences();
                    Report("shortcut-modifiers-resynchronized");
                }
                IReadOnlySet<int> activeModifiers = modifierState.ActiveModifiers;
                if (!activeModifiers.SetEquals(verifiedModifiers))
                {
                    CancelPendingSequences();
                    return CallNextHookEx(handle, code, message, data);
                }
                DateTimeOffset now = DateTimeOffset.UtcNow;
                bool normalTriggered = normalDetector.KeyDown(key, now, activeModifiers);
                bool translationTriggered = translationDetector.KeyDown(key, now, activeModifiers);
                if (normalDetector.IsPending || translationDetector.IsPending) Report("shortcut-sequence-started");
                if (key == 0x43 && !normalTriggered && !translationTriggered) clipboardSequenceBeforeCopy = GetClipboardSequenceNumber();
                if (normalTriggered || translationTriggered)
                {
                    Report("shortcut-activated");
                    nint source = GetForegroundWindow();
                    Triggered?.Invoke(source, GetClipboardSequenceNumber() != clipboardSequenceBeforeCopy, normalTriggered ? InvocationShortcut.Choose : InvocationShortcut.Translate);
                    suppressedKeys.Add(key);
                    return 1;
                }

                void Report(string stage) => Diagnostic?.Invoke(new KeyboardShortcutDiagnostic(
                    stage, key, scanCode, flags, inputTime, observedMask, ModifierMask(verifiedModifiers),
                    ModifierMask(modifierState.ActiveModifiers), windowsModifiers.Contains(0xA2), windowsModifiers.Contains(0xA3)));
            }
            else if (message == WmKeyUp || message == WmSysKeyUp)
            {
                normalDetector.KeyUp(key);
                translationDetector.KeyUp(key);
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

    private void CancelPendingSequences()
    {
        normalDetector.CancelPendingSequence();
        translationDetector.CancelPendingSequence();
    }

    private static int ModifierMask(IReadOnlySet<int> modifiers) =>
        (modifiers.Contains(0x11) ? 1 : 0) | (modifiers.Contains(0x10) ? 2 : 0) |
        (modifiers.Contains(0x12) ? 4 : 0) | (modifiers.Contains(0x5B) ? 8 : 0);

    /// <summary>
    /// Reads the held-state bit only. The hook runs before Windows updates the current key,
    /// so this snapshot verifies modifiers on non-modifier input; modifier transitions are tracked separately.
    /// </summary>
    private static IReadOnlySet<int> ReadWindowsModifiers() =>
        ModifierKeys.Where(candidate => (GetAsyncKeyState(candidate) & 0x8000) != 0).ToHashSet();

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

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

/// <summary>Captures modifier verification at a shortcut boundary without retaining typed text.</summary>
public sealed record KeyboardShortcutDiagnostic(string Stage, int VirtualKey, int ScanCode, int Flags, uint InputTime,
    int ObservedModifiers, int WindowsModifiers, int VerifiedModifiers, bool LeftCtrlDown, bool RightCtrlDown);
