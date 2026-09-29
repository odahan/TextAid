using System.Reflection;
using System.Text.Json;

namespace TextAid.Core;

/// <summary>Provides the initial editable JSON definitions supplied with TextAid.</summary>
public static class BuiltInActionCatalog
{
    private static readonly string[] FileNames = ["translate", "correct", "rewrite", "shorten", "expand", "simplify", "change-tone", "summarize", "answer-this-mail", "Synonymes", "humanize"];
    private static readonly IReadOnlyDictionary<string, string> DisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["translate"] = "Translate",
        ["correct"] = "Correct",
        ["rewrite"] = "Rewrite",
        ["shorten"] = "Shorten",
        ["expand"] = "Expand",
        ["simplify"] = "Simplify",
        ["change-tone"] = "Change tone",
        ["summarize"] = "Summarize",
        ["answer-this-mail"] = "Answer this mail",
        ["Synonymes"] = "Synonymes",
        ["humanize"] = "Humanize"
    };

    /// <summary>Gets or sets the optional UI-layer resolver for localized built-in action names.</summary>
    public static Func<string, string?>? DisplayNameResolver { get; set; }

    /// <summary>Gets the eight built-in actions in their normal session order.</summary>
    public static IReadOnlyList<ActionDefinition> Create() => FileNames.Select(ReadEmbeddedAction).ToArray();

    /// <summary>Gets the localized label for a built-in action, or its English fallback.</summary>
    public static string GetDisplayName(string id)
    {
        string? localized = DisplayNameResolver?.Invoke(id);
        return !string.IsNullOrWhiteSpace(localized)
            ? localized
            : DisplayNames.TryGetValue(id, out string? name) ? name : id;
    }

    /// <summary>Determines whether an action ID is supplied by TextAid.</summary>
    public static bool IsBuiltIn(string id) => DisplayNames.ContainsKey(id);

    /// <summary>Writes missing built-in action files and restores the system-owned Translate action.</summary>
    public static void EnsureCreated(string actionsDirectory)
    {
        Directory.CreateDirectory(actionsDirectory);
        foreach (ActionDefinition action in Create())
        {
            string path = Path.Combine(actionsDirectory, $"{action.Id}.json");
            if (action.IsReserved || !File.Exists(path))
            File.WriteAllText(path, JsonSerializer.Serialize(action, ActionJson.Options));
        }
    }

    private static ActionDefinition ReadEmbeddedAction(string fileName)
    {
        Assembly assembly = typeof(BuiltInActionCatalog).Assembly;
        using Stream stream = assembly.GetManifestResourceStream($"TextAid.Core.Actions.{fileName}.json")
            ?? throw new InvalidOperationException($"Built-in action resource '{fileName}' is missing.");
        using var reader = new StreamReader(stream);
        return JsonSerializer.Deserialize<ActionDefinition>(reader.ReadToEnd(), ActionJson.Options)
            ?? throw new InvalidOperationException($"Built-in action resource '{fileName}' is invalid.");
    }
}

/// <summary>Centralizes JSON options used by external action data.</summary>
internal static class ActionJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
