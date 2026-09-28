namespace TextAid.Core;

/// <summary>Validates external action definitions before they become active.</summary>
public static class ActionValidator
{
    /// <summary>Validates one action against the profiles available at this milestone.</summary>
    public static void Validate(ActionDefinition action, ISet<string> profileIds)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(profileIds);
        if (string.IsNullOrWhiteSpace(action.Id)) throw new InvalidOperationException("An action ID is required.");
        if (action.Id.Any(char.IsWhiteSpace)) throw new InvalidOperationException($"Action '{action.Id}' has an invalid ID.");
        if (!action.IsReserved && action.Id.Equals("translate", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The Translate action ID is reserved.");
        if (action.IsReserved && !action.Id.Equals("translate", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only the Translate action can be reserved.");
        if (string.IsNullOrWhiteSpace(action.PromptTemplate)) throw new InvalidOperationException($"Action '{action.Id}' requires a prompt template.");
        if (!profileIds.Contains(action.ProfileId)) throw new InvalidOperationException($"Action '{action.Id}' references an unknown profile.");
        if (action.TemperatureOverride is < 0 or > 2) throw new InvalidOperationException($"Action '{action.Id}' has an invalid temperature override.");
        if (string.IsNullOrWhiteSpace(action.OutputLanguageDefault)) throw new InvalidOperationException($"Action '{action.Id}' requires an output-language default.");
        if (!action.OutputLanguageDefault.Equals("Unchanged", StringComparison.OrdinalIgnoreCase) && !LanguageCatalog.IsSupported(action.OutputLanguageDefault))
            throw new InvalidOperationException($"Action '{action.Id}' has an invalid output-language default.");
        TemplateRenderer.ValidateTemplate(action.PromptTemplate);
    }

    /// <summary>Validates the complete action set, including unique IDs and the reserved Translate action.</summary>
    public static void ValidateAll(IReadOnlyCollection<ActionDefinition> actions, ISet<string> profileIds)
    {
        if (actions.Count == 0) throw new InvalidOperationException("At least one action is required.");
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int reservedTranslateCount = 0;
        foreach (ActionDefinition action in actions)
        {
            Validate(action, profileIds);
            if (!ids.Add(action.Id)) throw new InvalidOperationException($"Action ID '{action.Id}' is duplicated.");
            if (action.IsReserved && action.Id.Equals("translate", StringComparison.OrdinalIgnoreCase)) reservedTranslateCount++;
        }

        if (reservedTranslateCount != 1) throw new InvalidOperationException("Exactly one reserved Translate action is required.");
        ActionDefinition translate = actions.Single(action => action.IsReserved && action.Id.Equals("translate", StringComparison.OrdinalIgnoreCase));
        ActionDefinition expected = BuiltInActionCatalog.Create().Single(action => action.Id == "translate");
        if (translate != expected) throw new InvalidOperationException("The reserved Translate action cannot be modified.");
    }
}
