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

    /// <summary>Creates the service for a provider whose MAF options are not Ollama-specific.</summary>
    public MafTextTransformationService(Func<TextTransformationSettings, IChatClient> createChatClient, bool isOllamaClient, int? maxOutputTokens = null)
        : this(createChatClient)
    {
        this.isOllamaClient = isOllamaClient;
        this.maxOutputTokens = maxOutputTokens;
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
        using IChatClient chatClient = createChatClient(request.Settings);

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
        AgentResponse response = isOllamaClient
            ? await agent.RunAsync(request.InputText, cancellationToken: linkedSource.Token)
            : await agent.RunStreamingAsync(request.InputText, cancellationToken: linkedSource.Token)
                .ToAgentResponseAsync(linkedSource.Token);
        string result = response.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(result)) throw new InvalidOperationException("The local model returned an empty result.");
        return result;
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
