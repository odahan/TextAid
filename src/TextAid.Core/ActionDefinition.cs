using System.Text.Json.Serialization;

namespace TextAid.Core;

/// <summary>Describes one declarative TextAid transformation action.</summary>
public sealed record ActionDefinition(
    string Id,
    string? DisplayNameOverride,
    bool IsEnabled,
    string PromptTemplate,
    string ProfileId,
    float? TemperatureOverride,
    string OutputLanguageDefault,
    bool AskForUserInstructions,
    bool IsReserved = false)
{
    /// <summary>Returns the English fallback display name for a built-in action.</summary>
    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(DisplayNameOverride)
        ? BuiltInActionCatalog.GetDisplayName(Id)
        : DisplayNameOverride;
}
