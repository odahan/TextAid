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
    /// <summary>Gets the AI-detected language of the current input, if identification succeeded.</summary>
    public string? DetectedInputLanguage { get; private set; }
    /// <summary>Gets whether the current input is awaiting an AI language response.</summary>
    public bool IsDetectingInputLanguage { get; private set; }
    /// <summary>Gets whether detection completed, including an indeterminate response.</summary>
    public bool IsInputLanguageDetectionComplete { get; private set; }
    /// <summary>Gets whether the last detection request failed and can be retried.</summary>
    public bool InputLanguageDetectionFailed { get; private set; }
    /// <summary>Notifies the UI context that the current input's detection state changed.</summary>
    public event EventHandler? LanguageDetectionChanged;
    private CancellationTokenSource languageDetectionCancellation = new();
    private Task<string?>? languageDetectionTask;
    public string? ActionId { get; set; }
    private string outputLanguage = "Unchanged";
    private bool outputLanguageWasAutomatic;
    /// <summary>Gets or sets the one-invocation requested output language.</summary>
    public string OutputLanguage
    {
        get => outputLanguage;
        set
        {
            outputLanguage = value;
            outputLanguageWasAutomatic = false;
        }
    }

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

    /// <summary>Chooses a translation direction that must be reconsidered if the input changes.</summary>
    public void SelectAutomaticTranslationDestination(string detectedLanguage, string userLanguage, string preferredLanguage)
    {
        OutputLanguage = QuickTranslationRouting.SelectDestination(detectedLanguage, userLanguage, preferredLanguage);
        outputLanguageWasAutomatic = true;
    }

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

    /// <summary>Invalidates a result or pending generation when its request changes.</summary>
    public void InvalidateResult()
    {
        if (State == InvocationState.Transforming)
        {
            cancellation.Cancel();
            Generation++;
        }

        OutputText = null;
        if (State is InvocationState.Transforming or InvocationState.ResultReady) State = InvocationState.Ready;
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

    /// <summary>Updates the input and cancels any language request associated with its previous value.</summary>
    public void SetInputText(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.Equals(InputText, input, StringComparison.Ordinal)) return;
        InvalidateResult();
        InputText = input;
        if (outputLanguageWasAutomatic) OutputLanguage = "Unchanged";
        languageDetectionCancellation.Cancel();
        languageDetectionCancellation.Dispose();
        languageDetectionCancellation = new CancellationTokenSource();
        languageDetectionTask = null;
        DetectedInputLanguage = null;
        IsDetectingInputLanguage = false;
        IsInputLanguageDetectionComplete = false;
        InputLanguageDetectionFailed = false;
        LanguageDetectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Shares one asynchronous detection request per input and retries only failed requests.</summary>
    public Task<string?> DetectInputLanguageAsync(Func<string, CancellationToken, Task<string?>> detect)
    {
        ArgumentNullException.ThrowIfNull(detect);
        if (string.IsNullOrWhiteSpace(InputText)) return Task.FromResult<string?>(null);
        if (languageDetectionTask is null || languageDetectionTask.IsFaulted || languageDetectionTask.IsCanceled)
        {
            languageDetectionTask = DetectCurrentInputAsync(detect, InputText, languageDetectionCancellation.Token);
        }
        return languageDetectionTask;
    }

    /// <summary>Publishes a response on the calling context only while its input remains current.</summary>
    private async Task<string?> DetectCurrentInputAsync(Func<string, CancellationToken, Task<string?>> detect, string input, CancellationToken token)
    {
        IsDetectingInputLanguage = true;
        InputLanguageDetectionFailed = false;
        LanguageDetectionChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            string? language = await detect(input, token);
            token.ThrowIfCancellationRequested();
            DetectedInputLanguage = language;
            IsInputLanguageDetectionComplete = true;
            return language;
        }
        catch
        {
            if (!token.IsCancellationRequested) InputLanguageDetectionFailed = true;
            throw;
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                IsDetectingInputLanguage = false;
                LanguageDetectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Cancels pending detection and transformation work when the invocation closes.</summary>
    public void Dispose()
    {
        languageDetectionCancellation.Cancel();
        languageDetectionCancellation.Dispose();
        cancellation.Cancel();
        cancellation.Dispose();
    }
}
