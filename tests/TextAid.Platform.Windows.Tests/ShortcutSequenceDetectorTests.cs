using TextAid.Platform.Windows;

namespace TextAid.Platform.Windows.Tests;

public sealed class ShortcutSequenceDetectorTests
{
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
