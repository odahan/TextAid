using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TextAid.Platform.Windows;

/// <summary>Observes keyboard transitions while preserving all source application input.</summary>
public sealed class KeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private readonly DoubleCopyDetector detector = new(TimeSpan.FromMilliseconds(450));
    private readonly HookProcedure callback;
    private nint handle;

    public KeyboardHook()
    {
        callback = OnKeyboard;
        using Process process = Process.GetCurrentProcess();
        using ProcessModule? module = process.MainModule;
        handle = SetWindowsHookEx(WhKeyboardLl, callback, GetModuleHandle(module?.ModuleName), 0);
        if (handle == 0) throw new System.ComponentModel.Win32Exception();
    }

    public event Action<nint>? Triggered;

    private nint OnKeyboard(int code, nint message, nint data)
    {
        if (code >= 0)
        {
            int key = Marshal.ReadInt32(data);
            if (message == WmKeyDown || message == WmSysKeyDown)
            {
                if (detector.KeyDown(key, DateTimeOffset.UtcNow))
                {
                    nint source = GetForegroundWindow();
                    Triggered?.Invoke(source);
                }
            }
            else if (message == WmKeyUp || message == WmSysKeyUp) detector.KeyUp(key);
        }
        return CallNextHookEx(handle, code, message, data);
    }

    public void Dispose()
    {
        if (handle != 0) { UnhookWindowsHookEx(handle); handle = 0; }
        GC.SuppressFinalize(this);
    }

    private delegate nint HookProcedure(int code, nint message, nint data);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HookProcedure procedure, nint module, uint threadId);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hook, int code, nint message, nint data);
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? moduleName);
}
