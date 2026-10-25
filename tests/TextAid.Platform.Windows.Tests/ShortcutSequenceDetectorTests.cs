using TextAid.Platform.Windows;

namespace TextAid.Platform.Windows.Tests;

public sealed class ShortcutSequenceDetectorTests
{
    [Theory]
    [InlineData("Ctrl+C+T", 0x54)]
    [InlineData("Ctrl+C+C", 0x43)]
    public void PlainTyping_NeverTriggers(string shortcut, int finalKey)
    {
        var detector = new ShortcutSequenceDetector(shortcut, TimeSpan.FromSeconds(1));
        var modifiers = new HashSet<int>();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Assert.False(detector.KeyDown(0x43, now, modifiers));
        detector.KeyUp(0x43);
        Assert.False(detector.KeyDown(finalKey, now.AddMilliseconds(100), modifiers));
    }

    [Fact]
    public void ModifierReleaseAndRepress_CannotCompletePreviousSequence()
    {
        var detector = new ShortcutSequenceDetector("Ctrl+C+T", TimeSpan.FromSeconds(1));
        var modifiers = new HashSet<int> { 0x11 };
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Assert.False(detector.KeyDown(0x43, now, modifiers));
        detector.KeyUp(0x43);
        detector.CancelPendingSequence();
        Assert.False(detector.KeyDown(0x54, now.AddMilliseconds(100), modifiers));
    }

    [Fact]
    public void AutoRepeat_CannotCompleteDoubleCopy()
    {
        var detector = new ShortcutSequenceDetector("Ctrl+C+C", TimeSpan.FromSeconds(1));
        var modifiers = new HashSet<int> { 0x11 };
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Assert.False(detector.KeyDown(0x43, now, modifiers));
        Assert.False(detector.KeyDown(0x43, now.AddMilliseconds(100), modifiers));
        detector.KeyUp(0x43);
        Assert.True(detector.KeyDown(0x43, now.AddMilliseconds(200), modifiers));
    }

    [Fact]
    public void CtrlReleasedBeforeFinalKey_DoesNotTrigger()
    {
        var detector = new ShortcutSequenceDetector("Ctrl+C+T", TimeSpan.FromSeconds(1));
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Assert.False(detector.KeyDown(0x43, now, new HashSet<int> { 0x11 }));
        detector.KeyUp(0x43);
        Assert.False(detector.KeyDown(0x54, now.AddMilliseconds(100), new HashSet<int>()));
    }

    [Fact]
    public void ExpiredSequence_DoesNotTrigger()
    {
        var detector = new ShortcutSequenceDetector("Ctrl+C+T", TimeSpan.FromSeconds(1));
        var modifiers = new HashSet<int> { 0x11 };
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Assert.False(detector.KeyDown(0x43, now, modifiers));
        detector.KeyUp(0x43);
        Assert.False(detector.KeyDown(0x54, now.AddMilliseconds(1001), modifiers));
    }

    [Fact]
    public void DefaultTranslationSequence_RecognizesCtrlCThenT()
    {
        var detector = new ShortcutSequenceDetector("Ctrl+C+T", TimeSpan.FromMilliseconds(450));
        var modifiers = new HashSet<int> { 0x11 };
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Assert.False(detector.KeyDown(0x43, now, modifiers));
        detector.KeyUp(0x43);
        Assert.True(detector.KeyDown(0x54, now.AddMilliseconds(100), modifiers));
    }

    [Fact]
    public void TranslationSequence_AllowsAMeasuredPauseBetweenCopyAndT()
    {
        var detector = new ShortcutSequenceDetector("Ctrl+C+T", TimeSpan.FromSeconds(1));
        var modifiers = new HashSet<int> { 0x11 };
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Assert.False(detector.KeyDown(0x43, now, modifiers));
        detector.KeyUp(0x43);
        Assert.True(detector.KeyDown(0x54, now.AddMilliseconds(900), modifiers));
    }

    [Fact]
    public void DifferentModifiers_DoNotTriggerAConfiguredSequence()
    {
        var detector = new ShortcutSequenceDetector("Ctrl+C+T", TimeSpan.FromMilliseconds(450));
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var modifiers = new HashSet<int> { 0x11, 0x10 };

        Assert.False(detector.KeyDown(0x43, now, modifiers));
        detector.KeyUp(0x43);
        Assert.False(detector.KeyDown(0x54, now.AddMilliseconds(100), modifiers));
    }
}
