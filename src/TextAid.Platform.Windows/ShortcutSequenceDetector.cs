namespace TextAid.Platform.Windows;

/// <summary>Recognizes a two-key shortcut sequence while leaving all input available to Windows.</summary>
public sealed class ShortcutSequenceDetector
{
    private readonly HashSet<int> modifiers;
    private readonly int firstKey;
    private readonly int secondKey;
    private readonly TimeSpan maximumDelay;
    private readonly HashSet<int> downKeys = [];
    private DateTimeOffset? firstPressedAt;

    /// <summary>Creates a detector for a persisted TextAid shortcut such as Ctrl+C+T.</summary>
    public ShortcutSequenceDetector(string shortcut, TimeSpan maximumDelay)
    {
        string[] parts = shortcut.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) throw new ArgumentException("A shortcut needs two keys.", nameof(shortcut));
        firstKey = ToVirtualKey(parts[^2]);
        secondKey = ToVirtualKey(parts[^1]);
        modifiers = parts.Take(parts.Length - 2).Select(ToVirtualKey).ToHashSet();
        this.maximumDelay = maximumDelay;
    }

    /// <summary>Processes a key transition and returns true when the complete sequence was entered.</summary>
    public bool KeyDown(int key, DateTimeOffset now, IReadOnlySet<int> activeModifiers)
    {
        if (!downKeys.Add(key)) return false;
        if (!modifiers.SetEquals(activeModifiers)) { Reset(); return false; }
        if (firstPressedAt is null && key == firstKey) { firstPressedAt = now; return false; }
        bool matched = firstPressedAt is not null && key == secondKey && now - firstPressedAt <= maximumDelay;
        Reset();
        return matched;
    }

    /// <summary>Releases a key so key-repeat messages cannot complete a sequence.</summary>
    public void KeyUp(int key) => downKeys.Remove(key);

    private void Reset() => firstPressedAt = null;

    private static int ToVirtualKey(string token) => token.ToUpperInvariant() switch
    {
        "CTRL" or "CONTROL" => 0x11,
        "SHIFT" => 0x10,
        "ALT" => 0x12,
        "WIN" or "WINDOWS" => 0x5B,
        [char value] when char.IsLetterOrDigit(value) => char.ToUpperInvariant(value),
        _ => throw new ArgumentException($"Unsupported shortcut key '{token}'.")
    };
}
