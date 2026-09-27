using System.Text.Json;

namespace TextAid.Core;

/// <summary>Loads the external action files located beside the running application.</summary>
public sealed class ActionLoader
{
    private readonly ISet<string> profileIds;
    private readonly string actionsDirectory;

    /// <summary>Creates a loader for the currently known model profiles.</summary>
    public ActionLoader(IEnumerable<string>? profileIds = null, string? actionsDirectory = null)
    {
        this.profileIds = new HashSet<string>(profileIds ?? ["local-default"], StringComparer.OrdinalIgnoreCase);
        this.actionsDirectory = actionsDirectory ?? ActionsDirectory;
    }

    /// <summary>Gets the visible action directory adjacent to the running executable.</summary>
    public static string ActionsDirectory => Path.Combine(AppContext.BaseDirectory, "actions");

    /// <summary>Creates missing built-ins and loads a completely valid action set.</summary>
    public IReadOnlyList<ActionDefinition> Load()
    {
        BuiltInActionCatalog.EnsureCreated(actionsDirectory);
        var actions = new List<ActionDefinition>();
        foreach (string path in Directory.EnumerateFiles(actionsDirectory, "*.json", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            ActionDefinition? action;
            try
            {
                action = JsonSerializer.Deserialize<ActionDefinition>(File.ReadAllText(path), ActionJson.Options);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException($"Action file '{Path.GetFileName(path)}' is invalid.", exception);
            }

            if (action is null) throw new InvalidOperationException($"Action file '{Path.GetFileName(path)}' is empty.");
            actions.Add(action);
        }

        ActionValidator.ValidateAll(actions, profileIds);
        return actions.Where(action => action.IsEnabled).ToArray();
    }
}
