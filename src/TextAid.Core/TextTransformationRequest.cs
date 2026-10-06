namespace TextAid.Core;

/// <summary>Specifies the requested level of model reasoning when supported by a provider.</summary>
public enum ThinkingMode
{
    Off,
    Low,
    Medium,
    High
}

/// <summary>Describes the single text transformation requested by a session.</summary>
public sealed record TextTransformationRequest(
    string InputText,
    string Instruction,
    TextTransformationSettings Settings);

/// <summary>Contains the local Ollama settings needed for one transformation.</summary>
public sealed record TextTransformationSettings(
    string Endpoint,
    string Model,
    float Temperature,
    TimeSpan Timeout,
    int ContextSize,
    ThinkingMode Thinking)
{
    /// <summary>Indicates whether a model has been selected by the user.</summary>
    public bool HasModel => !string.IsNullOrWhiteSpace(Model);
}
