namespace TextAid.Core;

public enum InvocationState
{
    Captured,
    Ready,
    Transforming,
    ResultReady,
    Replaced,
    Copied,
    Cancelled,
    Failed
}

/// <summary>Represents one captured source selection and its transaction state.</summary>
public sealed class InvocationSession(nint sourceWindow, string inputText) : IDisposable
{
    public Guid Id { get; } = Guid.NewGuid();
    public nint SourceWindow { get; private set; } = sourceWindow;
    public string InputText { get; set; } = inputText;
    public string? ActionId { get; set; }
    public string? SupplementaryInstructions { get; set; }
    public string? OutputText { get; set; }
    public InvocationState State { get; set; } = InvocationState.Captured;
    public CancellationTokenSource Cancellation { get; } = new();

    /// <summary>Resets the session for new manual input without retaining a replacement target.</summary>
    public void ResetForNewInput()
    {
        SourceWindow = 0;
        InputText = string.Empty;
        ActionId = null;
        SupplementaryInstructions = null;
        OutputText = null;
        State = InvocationState.Ready;
    }

    public void Dispose() => Cancellation.Dispose();
}
