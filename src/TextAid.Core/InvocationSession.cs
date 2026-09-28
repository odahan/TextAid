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
    /// <summary>Gets or sets the one-invocation requested output language.</summary>
    public string OutputLanguage { get; set; } = "Unchanged";
    /// <summary>Gets or sets whether this invocation requests Markdown-formatted output.</summary>
    public bool MarkdownOutputEnabled { get; set; }
    public string? SupplementaryInstructions { get; set; }
    public string? OutputText { get; set; }
    public InvocationState State { get; set; } = InvocationState.Captured;
    /// <summary>Indicates whether the current generation produced a result that may be copied or replaced.</summary>
    public bool HasCurrentResult => State == InvocationState.ResultReady && !string.IsNullOrEmpty(OutputText);
    private CancellationTokenSource cancellation = new();
    /// <summary>Gets the cancellation source for the current generation.</summary>
    public CancellationTokenSource Cancellation => cancellation;
    /// <summary>Gets the monotonically increasing generation number.</summary>
    public int Generation { get; private set; }

    /// <summary>Cancels any pending generation and makes a fresh result authoritative.</summary>
    public int StartNewGeneration()
    {
        cancellation.Cancel();
        cancellation.Dispose();
        cancellation = new CancellationTokenSource();
        OutputText = null;
        State = InvocationState.Transforming;
        return ++Generation;
    }

    /// <summary>Publishes a result only when it belongs to the currently authoritative generation.</summary>
    public bool TryCompleteGeneration(int generation, string output)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(output);
        if (generation != Generation || State != InvocationState.Transforming) return false;
        OutputText = output;
        State = InvocationState.ResultReady;
        return true;
    }

    /// <summary>Resets the session for new manual input without retaining a replacement target.</summary>
    public void ResetForNewInput()
    {
        SourceWindow = 0;
        InputText = string.Empty;
        ActionId = null;
        MarkdownOutputEnabled = false;
        SupplementaryInstructions = null;
        OutputText = null;
        State = InvocationState.Ready;
    }

    public void Dispose() => cancellation.Dispose();
}
