using TextAid.Platform.Windows;

namespace TextAid.Platform.Windows.Tests;

public sealed class KeyboardModifierStateTests
{
    [Fact]
    public void MissedCtrlRelease_IsRemovedBeforeTypingCanTriggerShortcut()
    {
        var state = new KeyboardModifierState();
        var detector = new ShortcutSequenceDetector("Ctrl+C+T", TimeSpan.FromSeconds(1));
        state.Update(0xA2, true);

        Assert.True(state.Reconcile(new HashSet<int>()));
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Assert.False(detector.KeyDown(0x43, now, state.ActiveModifiers));
        detector.KeyUp(0x43);
        Assert.False(detector.KeyDown(0x54, now.AddMilliseconds(100), state.ActiveModifiers));
        Assert.Empty(state.ActiveModifiers);
    }

    [Fact]
    public void ReleasingOneCtrl_PreservesTheOtherCtrl()
    {
        var state = new KeyboardModifierState();
        state.Update(0xA2, true);
        state.Update(0xA3, true);
        state.Update(0xA2, false);

        Assert.False(state.Reconcile(new HashSet<int> { 0xA3 }));
        Assert.Contains(0x11, state.ActiveModifiers);
    }

    [Fact]
    public void WindowsSnapshot_DoesNotArmUnobservedCtrlPress()
    {
        var state = new KeyboardModifierState();

        Assert.False(state.Reconcile(new HashSet<int> { 0xA2 }));
        Assert.Empty(state.ActiveModifiers);
    }

    [Fact]
    public void Reconciliation_RemovesStaleSideEvenWhenOtherCtrlIsDown()
    {
        var state = new KeyboardModifierState();
        state.Update(0xA2, true);

        Assert.True(state.Reconcile(new HashSet<int> { 0xA3 }));
        Assert.Empty(state.ActiveModifiers);
    }

    [Fact]
    public void RightWindowsKey_IsNormalizedForShortcutFiltering()
    {
        var state = new KeyboardModifierState();
        state.Update(0x5C, true);

        Assert.Contains(0x5B, state.ActiveModifiers);
        state.Update(0x5C, false);
        Assert.Empty(state.ActiveModifiers);
    }
}
