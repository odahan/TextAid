namespace TextAid.Platform.Windows;

/// <summary>Tracks observed modifier transitions and removes keys Windows no longer reports as held.</summary>
public sealed class KeyboardModifierState
{
    private readonly HashSet<int> pressedKeys = [];

    /// <summary>Gets the normalized modifiers confirmed by observed keyboard transitions.</summary>
    public IReadOnlySet<int> ActiveModifiers => NormalizeModifiers(pressedKeys);

    /// <summary>Combines left and right variants for exact shortcut modifier comparison.</summary>
    public static IReadOnlySet<int> NormalizeModifiers(IEnumerable<int> keys) => keys.Select(Normalize).ToHashSet();

    /// <summary>Updates one physical modifier without conflating its left and right variants.</summary>
    public void Update(int key, bool isDown)
    {
        if (!IsModifier(key)) return;
        if (isDown) pressedKeys.Add(key);
        else pressedKeys.Remove(key);
    }

    /// <summary>Removes stale modifiers without arming shortcuts from unobserved presses.</summary>
    public bool Reconcile(IReadOnlySet<int> windowsPressedKeys) =>
        pressedKeys.RemoveWhere(key => !windowsPressedKeys.Contains(key)) > 0;

    /// <summary>Identifies supported modifier virtual keys, including both Windows keys.</summary>
    public static bool IsModifier(int key) => key is 0x10 or 0xA0 or 0xA1 or 0x11 or 0xA2 or 0xA3 or 0x12 or 0xA4 or 0xA5 or 0x5B or 0x5C;

    private static int Normalize(int key) => key switch
    {
        0xA0 or 0xA1 => 0x10,
        0xA2 or 0xA3 => 0x11,
        0xA4 or 0xA5 => 0x12,
        0x5C => 0x5B,
        _ => key
    };
}
