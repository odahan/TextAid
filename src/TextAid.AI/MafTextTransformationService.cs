using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OllamaSharp.Models;
using TextAid.Core;

namespace TextAid.AI;

/// <summary>Runs one complete text transformation through Microsoft Agent Framework and IChatClient.</summary>
public sealed class MafTextTransformationService(Func<TextTransformationSettings, IChatClient> createChatClient)
    : ITextTransformationService
{
    private static readonly TimeSpan MinimumExternalTimeout = TimeSpan.FromMinutes(10);
    private readonly bool isOllamaClient = true;
    private readonly int? maxOutputTokens;
    private readonly Action<InferenceDiagnostics>? reportDiagnostics;
    private readonly Action<string>? reportPreview;

    /// <summary>Creates the service for a provider whose MAF options are not Ollama-specific.</summary>
    public MafTextTransformationService(Func<TextTransformationSettings, IChatClient> createChatClient, bool isOllamaClient, int? maxOutputTokens = null,
        Action<InferenceDiagnostics>? reportDiagnostics = null, Action<string>? reportPreview = null)
        : this(createChatClient)
    {
        this.isOllamaClient = isOllamaClient;
        this.maxOutputTokens = maxOutputTokens;
        this.reportDiagnostics = reportDiagnostics;
        this.reportPreview = reportPreview;
    }
    /// <inheritdoc />
    public async Task<string> TransformAsync(TextTransformationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.InputText)) throw new ArgumentException("Input text is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Instruction)) throw new ArgumentException("An instruction is required.", nameof(request));
        if (!request.Settings.HasModel) throw new InvalidOperationException("No Ollama model is selected.");
        if (!Uri.TryCreate(request.Settings.Endpoint, UriKind.Absolute, out _)) throw new InvalidOperationException("The Ollama endpoint is invalid.");

        TimeSpan effectiveTimeout = isOllamaClient
            ? request.Settings.Timeout
            : request.Settings.Timeout < MinimumExternalTimeout
                ? MinimumExternalTimeout
                : request.Settings.Timeout;
        using var timeoutSource = new CancellationTokenSource(effectiveTimeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        using var chatClient = new DiagnosticChatClient(createChatClient(request.Settings));

        var chatOptions = new ChatOptions { Instructions = request.Instruction };
        if (isOllamaClient)
        {
            int outputBudget = maxOutputTokens is > 0
                ? Math.Min(maxOutputTokens.Value, Math.Max(1024, request.Settings.ContextSize / 2))
                : Math.Max(1024, request.Settings.ContextSize / 2);
            chatOptions.MaxOutputTokens = outputBudget;
            chatOptions.Temperature = request.Settings.Temperature;
            chatOptions.Reasoning = new ReasoningOptions
            {
                Effort = ToReasoningEffort(request.Settings.Thinking),
                Output = ReasoningOutput.None
            };
            chatOptions.ToolMode = ChatToolMode.None;
            chatOptions.AddOllamaOption(OllamaOption.NumCtx, request.Settings.ContextSize);
            chatOptions.AddOllamaOption(OllamaOption.MaxOutputTokens, outputBudget);
        }
        else if (maxOutputTokens is > 0) chatOptions.MaxOutputTokens = maxOutputTokens;
        var agent = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                ChatOptions = chatOptions,
                Name = "TextAidRewrite"
            });
        bool completed = false;
        bool cancelled = false;
        try
        {
            AgentResponse response = await ObservePreviewAsync(
                agent.RunStreamingAsync(request.InputText, cancellationToken: linkedSource.Token), linkedSource.Token)
                .ToAgentResponseAsync(linkedSource.Token);
            string result = response.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(result)) throw new InvalidOperationException("The local model returned an empty result.");
            completed = true;
            return result;
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            throw;
        }
        finally
        {
            reportDiagnostics?.Invoke(chatClient.Snapshot(completed, cancelled));
        }
    }

    /// <summary>Reports cumulative visible text while preserving all provider updates for final aggregation.</summary>
    private async IAsyncEnumerable<AgentResponseUpdate> ObservePreviewAsync(
        IAsyncEnumerable<AgentResponseUpdate> updates,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        var preview = new System.Text.StringBuilder();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        await foreach (AgentResponseUpdate update in updates.WithCancellation(token).ConfigureAwait(false))
        {
            if (reportPreview is not null && !string.IsNullOrEmpty(update.Text))
            {
                preview.Append(update.Text);
                if (watch.ElapsedMilliseconds >= 100 || preview.Length == update.Text.Length)
                {
                    reportPreview(preview.ToString());
                    watch.Restart();
                }
            }
            yield return update;
        }
    }

    /// <summary>Maps the TextAid thinking choice to the provider-neutral MAF reasoning option.</summary>
    private static ReasoningEffort ToReasoningEffort(ThinkingMode thinking) => thinking switch
    {
        ThinkingMode.Off => ReasoningEffort.None,
        ThinkingMode.Low => ReasoningEffort.Low,
        ThinkingMode.Medium => ReasoningEffort.Medium,
        ThinkingMode.High => ReasoningEffort.High,
        _ => ReasoningEffort.None
    };
}
