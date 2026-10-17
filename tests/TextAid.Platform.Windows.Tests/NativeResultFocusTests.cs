using System.Runtime.InteropServices;
using TextAid.Platform.Windows;

namespace TextAid.Platform.Windows.Tests;

[CollectionDefinition("Native replacement focus", DisableParallelization = true)]
public sealed class NativeReplacementFocusCollection;

[Collection("Native replacement focus")]
public sealed class NativeResultFocusTests
{
    [Fact]
    public void RestoringAnActiveWindowPreservesItsFocusedChild()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            nint previousForeground = GetForegroundWindow();
            nint host = 0;
            try
            {
                host = CreateWindowEx(0, "STATIC", "TextAid focus regression test", 0x00CF0000,
                    0, 0, 320, 140, 0, 0, 0, 0);
                Assert.NotEqual(0, host);
                nint editor = CreateWindowEx(0, "EDIT", string.Empty, 0x50000000,
                    10, 10, 280, 30, host, 0, 0, 0);
                Assert.NotEqual(0, editor);
                ShowWindow(host, 5);
                Assert.True(WindowsShell.TryActivateWindow(host), "The test host could not acquire foreground activation.");
                SetFocus(editor);
                Assert.Equal(editor, GetFocus());

                var native = new Win32ResultActions();

                Assert.True(native.RestoreAndFocus(host), "The replacement target could not be activated.");
                Assert.True(native.IsFocused(host), "The activated target lost keyboard focus.");
                Assert.Equal(editor, GetFocus());
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                if (host != 0) DestroyWindow(host);
                if (previousForeground != 0) SetForegroundWindow(previousForeground);
            }
        });
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Native focus test did not complete.");
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateWindowEx(uint extendedStyle, string className, string title, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint window, int command);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")]
    private static extern nint SetFocus(nint window);
    [DllImport("user32.dll")]
    private static extern nint GetFocus();
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
}
