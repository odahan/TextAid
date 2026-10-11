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
    bool IsReserved = false,
    string? UserInstructionsQuestion = null,
    [property: JsonConverter(typeof(JsonStringEnumConverter<ThinkingMode>))] ThinkingMode Thinking = ThinkingMode.Off,
    int? ContextSizeOverride = null)
{
    /// <summary>Applies action inference choices while inheriting the profile context when no override is set.</summary>
    public TextTransformationSettings ApplyInferenceSettings(TextTransformationSettings settings) => settings with
    {
        Temperature = TemperatureOverride ?? settings.Temperature,
        Thinking = Thinking,
        ContextSize = ContextSizeOverride ?? settings.ContextSize
    };

    /// <summary>Returns the English fallback display name for a built-in action.</summary>
    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(DisplayNameOverride)
        ? BuiltInActionCatalog.GetDisplayName(Id)
        : DisplayNameOverride;
}
