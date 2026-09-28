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
        return LoadAll().Where(action => action.IsEnabled).ToArray();
    }

    /// <summary>Loads every valid action, including disabled actions for the editor.</summary>
    public IReadOnlyList<ActionDefinition> LoadAll()
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
        return actions;
    }

    /// <summary>Atomically writes one valid action and verifies the complete active set before replacement.</summary>
    public void Save(ActionDefinition action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Directory.CreateDirectory(actionsDirectory);
        string destination = Path.Combine(actionsDirectory, $"{action.Id}.json");
        string temporary = destination + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(action, ActionJson.Options));
        try
        {
            var proposed = LoadAll().Where(existing => !existing.Id.Equals(action.Id, StringComparison.OrdinalIgnoreCase)).Append(action).ToArray();
            ActionValidator.ValidateAll(proposed, profileIds);
            File.Move(temporary, destination, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    /// <summary>Deletes a non-reserved action only when the remaining action set is valid.</summary>
    public void Delete(string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        IReadOnlyList<ActionDefinition> existing = LoadAll();
        ActionDefinition action = existing.SingleOrDefault(candidate => candidate.Id.Equals(actionId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("The selected action no longer exists.");
        if (action.IsReserved) throw new InvalidOperationException("The reserved Translate action cannot be deleted.");
        ActionValidator.ValidateAll(existing.Where(candidate => !candidate.Id.Equals(actionId, StringComparison.OrdinalIgnoreCase)).ToArray(), profileIds);
        File.Delete(Path.Combine(actionsDirectory, $"{action.Id}.json"));
    }

    /// <summary>Gets the writable action directory used by this loader.</summary>
    public string DirectoryPath => actionsDirectory;
}
