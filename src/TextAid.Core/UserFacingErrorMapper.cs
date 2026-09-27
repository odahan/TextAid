namespace TextAid.Core;

/// <summary>Maps technical failure categories to clear recovery guidance for the user.</summary>
public enum UserFacingFailure
{
    Clipboard,
    Configuration,
    Provider,
    Model,
    Cancellation,
    SourceWindow,
    Paste,
    Localization
}

/// <summary>Provides stable user-facing errors without exposing transport, COM, or Win32 details.</summary>
public static class UserFacingErrorMapper
{
    /// <summary>Returns a recoverable message for a known failure category.</summary>
    public static string GetMessage(UserFacingFailure failure) => failure switch
    {
        UserFacingFailure.Clipboard => "Unable to access the clipboard. Close applications that may be using it, then try again.",
        UserFacingFailure.Configuration => "TextAid could not use this configuration. Review the connection and model settings, then save again.",
        UserFacingFailure.Provider => "TextAid could not reach the selected provider. Check that it is running and reachable, then try again.",
        UserFacingFailure.Model => "The selected model is unavailable. Choose an installed model in Settings, then try again.",
        UserFacingFailure.Cancellation => "The transformation was cancelled. You can edit the text and start it again.",
        UserFacingFailure.SourceWindow => "TextAid could not safely return to the original window. Copy the result and paste it yourself.",
        UserFacingFailure.Paste => "TextAid could not paste safely. The result remains in the clipboard; paste it manually where you need it.",
        UserFacingFailure.Localization => "TextAid could not load the selected language. English is being used instead.",
        _ => "TextAid could not complete that step. Review the settings and try again."
    };
}
