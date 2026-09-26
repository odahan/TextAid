namespace TextAid.Platform.Windows;

/// <summary>Tracks physical key transitions for the unsuppressed double-copy gesture.</summary>
public sealed class DoubleCopyDetector(TimeSpan maximumDelay)
{
    private DateTimeOffset? lastCopy;
    private bool cIsDown;
    private bool controlIsDown;
    private bool armed;

    public bool KeyDown(int virtualKey, DateTimeOffset now)
    {
        if (IsControl(virtualKey))
        {
            controlIsDown = true;
            return false;
        }

        if (virtualKey == 0x43)
        {
            if (cIsDown) return false;
            cIsDown = true;
            if (!controlIsDown)
            {
                ResetGesture();
                return false;
            }

            bool trigger = armed && lastCopy.HasValue && now - lastCopy.Value <= maximumDelay;
            lastCopy = now;
            armed = !trigger;
            return trigger;
        }

        if (virtualKey is not (0x10 or 0xA0 or 0xA1)) ResetGesture();
        return false;
    }

    public void KeyUp(int virtualKey)
    {
        if (virtualKey == 0x43) cIsDown = false;
        if (IsControl(virtualKey))
        {
            controlIsDown = false;
            ResetGesture();
        }
    }

    private static bool IsControl(int virtualKey) => virtualKey is 0x11 or 0xA2 or 0xA3;

    private void ResetGesture()
    {
        armed = false;
        lastCopy = null;
    }
}
