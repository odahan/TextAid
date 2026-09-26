using TextAid.Platform.Windows;

namespace TextAid.Platform.Windows.Tests;

public sealed class ResultActionsTests
{
    [Fact]
    public void InvalidCapturedTargetNeverRestoresFocusOrPastes()
    {
        var native = new FakeWindowsResultActions { WindowExists = false };

        ReplaceResult result = new ResultActions(native).TryReplace((nint)42);

        Assert.Equal(ReplaceResult.InvalidTarget, result);
        Assert.Equal(0, native.RestoreAttempts);
        Assert.Equal(0, native.PasteAttempts);
    }

    [Fact]
    public void UnfocusedRestoredTargetNeverPastes()
    {
        var native = new FakeWindowsResultActions { Focused = false };

        ReplaceResult result = new ResultActions(native).TryReplace((nint)42);

        Assert.Equal(ReplaceResult.FocusFailed, result);
        Assert.Equal(1, native.RestoreAttempts);
        Assert.Equal(0, native.PasteAttempts);
    }

    [Fact]
    public void ReleasedModifiersPermitPasteIntoCapturedTarget()
    {
        var native = new FakeWindowsResultActions();

        ReplaceResult result = new ResultActions(native).TryReplace((nint)42);

        Assert.Equal(ReplaceResult.Replaced, result);
        Assert.Equal(1, native.RestoreAttempts);
        Assert.Equal(1, native.PasteAttempts);
    }

    [Fact]
    public void PressedModifiersPreventPaste()
    {
        var native = new FakeWindowsResultActions { ModifiersReleased = false };

        ReplaceResult result = new ResultActions(native).TryReplace((nint)42);

        Assert.Equal(ReplaceResult.ModifiersStillPressed, result);
        Assert.Equal(0, native.PasteAttempts);
    }

    [Fact]
    public void CopyDoesNotRestoreFocusOrPaste()
    {
        var native = new FakeWindowsResultActions();

        new ResultActions(native).Copy();

        Assert.Equal(0, native.RestoreAttempts);
        Assert.Equal(0, native.PasteAttempts);
    }

    private sealed class FakeWindowsResultActions : IWindowsResultActions
    {
        public bool WindowExists { get; init; } = true;
        public bool Focused { get; init; } = true;
        public bool ModifiersReleased { get; init; } = true;
        public int RestoreAttempts { get; private set; }
        public int PasteAttempts { get; private set; }

        public bool IsWindow(nint window) => WindowExists;
        public bool RestoreAndFocus(nint window)
        {
            RestoreAttempts++;
            return true;
        }

        public bool IsFocused(nint window) => Focused;
        public bool AreModifiersReleased() => ModifiersReleased;
        public bool SendPaste()
        {
            PasteAttempts++;
            return true;
        }
    }
}
