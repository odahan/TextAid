namespace TextAid.Core;

public enum InvocationState
{
    Captured,
    Ready,
    Transforming,
    ResultReady,
    Accepted,
    Cancelled,
    Failed
}

/// <summary>Represents one captured source selection and its transaction state.</summary>
public sealed class InvocationSession(nint sourceWindow, string inputText) : IDisposable
{
    public Guid Id { get; } = Guid.NewGuid();
    public nint SourceWindow { get; } = sourceWindow;
    public string InputText { get; } = inputText;
    public string? ActionId { get; set; }
    public IReadOnlyDictionary<string, string> Parameters { get; } = new Dictionary<string, string>();
    public string? OutputText { get; set; }
    public InvocationState State { get; set; } = InvocationState.Captured;
    public CancellationTokenSource Cancellation { get; } = new();

    public void Dispose() => Cancellation.Dispose();
}
