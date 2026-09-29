using System.Windows;

namespace TextAid.App.Localization;

/// <summary>Resolves the active UI resource for view-model status text.</summary>
internal static class UiStrings
{
    public static string Get(string key) => Application.Current?.TryFindResource(key) as string ?? key;

    public static string? TryGet(string key)
    {
        var value = Get(key);
        return value.Equals(key, StringComparison.Ordinal) ? null : value;
    }
}
