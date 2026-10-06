using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using OllamaSharp.Models.Chat;

namespace TextAid.AI;

/// <summary>Contains content-free timings for one provider request, including incomplete requests.</summary>
public sealed record InferenceDiagnostics(
    double ElapsedMilliseconds,
    double? FirstResponseMilliseconds,
    double? TotalMilliseconds,
    double? LoadMilliseconds,
    double? PromptMilliseconds,
    double? GenerationMilliseconds,
    long? InputTokens,
    long? OutputTokens,
    bool Completed,
    bool Cancelled);

/// <summary>Observes provider timings without retaining prompts, output text, or credentials.</summary>
internal sealed class DiagnosticChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    private readonly Stopwatch watch = Stopwatch.StartNew();
    private double? firstResponse;
    private ChatDoneResponseStream? done;

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
        {
            firstResponse ??= watch.Elapsed.TotalMilliseconds;
            if (update.RawRepresentation is ChatDoneResponseStream completion) done = completion;
            yield return update;
        }
    }

    /// <summary>Returns observed durations; absent server metrics remain unknown after an interrupted request.</summary>
    public InferenceDiagnostics Snapshot(bool completed, bool cancelled) => new(
        watch.Elapsed.TotalMilliseconds, firstResponse,
        done?.TotalDuration / 1_000_000d, done?.LoadDuration / 1_000_000d,
        done?.PromptEvalDuration / 1_000_000d, done?.EvalDuration / 1_000_000d,
        done?.PromptEvalCount, done?.EvalCount, completed, cancelled);
}
