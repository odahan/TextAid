using TextAid.Platform.Windows;

namespace TextAid.Platform.Windows.Tests;

public class DoubleCopyDetectorTests
{
    private static readonly DateTimeOffset Start = new(2026, 9, 26, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void OrdinaryCopyDoesNotTrigger()
    {
        var detector = NewDetector();
        detector.KeyDown(0xA2, Start);
        Assert.False(detector.KeyDown(0x43, Start.AddMilliseconds(10)));
        detector.KeyUp(0x43);
        detector.KeyUp(0xA2);
    }

    [Fact]
    public void DoubleCopyTriggersOnSecondPhysicalPress()
    {
        var detector = NewDetector();
        detector.KeyDown(0xA2, Start);
        Assert.False(detector.KeyDown(0x43, Start.AddMilliseconds(10)));
        detector.KeyUp(0x43);
        Assert.True(detector.KeyDown(0x43, Start.AddMilliseconds(200)));
    }

    [Fact]
    public void RightControlAlsoTriggers()
    {
        var detector = NewDetector();
        detector.KeyDown(0xA3, Start);
        detector.KeyDown(0x43, Start.AddMilliseconds(10));
        detector.KeyUp(0x43);
        Assert.True(detector.KeyDown(0x43, Start.AddMilliseconds(200)));
    }

    [Fact]
    public void KeyRepeatCannotTrigger()
    {
        var detector = NewDetector();
        detector.KeyDown(0xA2, Start);
        Assert.False(detector.KeyDown(0x43, Start.AddMilliseconds(10)));
        Assert.False(detector.KeyDown(0x43, Start.AddMilliseconds(100)));
    }

    [Fact]
    public void ExpiredGestureDoesNotTrigger()
    {
        var detector = NewDetector();
        detector.KeyDown(0xA2, Start);
        detector.KeyDown(0x43, Start.AddMilliseconds(10));
        detector.KeyUp(0x43);
        Assert.False(detector.KeyDown(0x43, Start.AddMilliseconds(461)));
    }

    [Fact]
    public void InterveningKeyResetsGesture()
    {
        var detector = NewDetector();
        detector.KeyDown(0xA2, Start);
        detector.KeyDown(0x43, Start.AddMilliseconds(10));
        detector.KeyUp(0x43);
        detector.KeyDown(0x56, Start.AddMilliseconds(50));
        Assert.False(detector.KeyDown(0x43, Start.AddMilliseconds(100)));
    }

    [Fact]
    public void TripleCTriggersOnce()
    {
        var detector = NewDetector();
        detector.KeyDown(0xA2, Start);
        Assert.False(detector.KeyDown(0x43, Start.AddMilliseconds(10)));
        detector.KeyUp(0x43);
        Assert.True(detector.KeyDown(0x43, Start.AddMilliseconds(100)));
        detector.KeyUp(0x43);
        Assert.False(detector.KeyDown(0x43, Start.AddMilliseconds(200)));
    }

    [Fact]
    public void ReleasedControlResetsGesture()
    {
        var detector = NewDetector();
        detector.KeyDown(0xA2, Start);
        detector.KeyDown(0x43, Start.AddMilliseconds(10));
        detector.KeyUp(0x43);
        detector.KeyUp(0xA2);
        detector.KeyDown(0xA2, Start.AddMilliseconds(50));
        Assert.False(detector.KeyDown(0x43, Start.AddMilliseconds(100)));
    }

    [Fact]
    public void CWithoutControlCannotArmGesture()
    {
        var detector = NewDetector();
        Assert.False(detector.KeyDown(0x43, Start));
        detector.KeyUp(0x43);
        detector.KeyDown(0xA2, Start.AddMilliseconds(20));
        Assert.False(detector.KeyDown(0x43, Start.AddMilliseconds(100)));
    }

    private static DoubleCopyDetector NewDetector() => new(TimeSpan.FromMilliseconds(450));
}
