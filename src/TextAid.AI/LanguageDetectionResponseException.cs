namespace TextAid.AI;

/// <summary>Distinguishes an invalid classification response from a provider connection failure.</summary>
public sealed class LanguageDetectionResponseException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);
