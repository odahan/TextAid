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
    public string InputText { get; private set; } = inputText;
    /// <summary>Gets the current locally detected input language when evidence is sufficient.</summary>
    public string? DetectedInputLanguage { get; private set; } = TextLanguageDetector.Detect(inputText);
    public string? ActionId { get; set; }
    /// <summary>Gets or sets the one-invocation requested output language.</summary>
    public string OutputLanguage { get; set; } = "Unchanged";
    /// <summary>Gets or sets whether this invocation requests Markdown-formatted output.</summary>
    public bool MarkdownOutputEnabled { get; set; }
    public string? SupplementaryInstructions { get; set; }
    /// <summary>Indicates that the current input originated from an isolated Free invocation.</summary>
    public bool IsFreeMode { get; private set; }
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
        SetInputText(string.Empty);
        ActionId = null;
        MarkdownOutputEnabled = false;
        SupplementaryInstructions = null;
        IsFreeMode = false;
        OutputText = null;
        State = InvocationState.Ready;
    }

    /// <summary>Prepares an isolated Free invocation without a replacement target or supplementary guidance.</summary>
    public void StartFreeInput(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        SourceWindow = 0;
        SetInputText(input);
        SupplementaryInstructions = null;
        IsFreeMode = true;
        OutputText = null;
        State = InvocationState.Ready;
    }

    /// <summary>Returns the current input to standard action processing without changing its text.</summary>
    public void UseStandardActionMode() => IsFreeMode = false;

    /// <summary>Updates the current session input and its corresponding local language detection.</summary>
    public void SetInputText(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        InputText = input;
        DetectedInputLanguage = TextLanguageDetector.Detect(input);
    }

    public void Dispose() => cancellation.Dispose();
}
