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
    /// <inheritdoc />
    public async Task<string> TransformAsync(TextTransformationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.InputText)) throw new ArgumentException("Input text is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Instruction)) throw new ArgumentException("An instruction is required.", nameof(request));
        if (!request.Settings.HasModel) throw new InvalidOperationException("No Ollama model is selected.");
        if (!Uri.TryCreate(request.Settings.Endpoint, UriKind.Absolute, out _)) throw new InvalidOperationException("The Ollama endpoint is invalid.");

        using var timeoutSource = new CancellationTokenSource(request.Settings.Timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        using IChatClient chatClient = createChatClient(request.Settings);

        var chatOptions = new ChatOptions
        {
            Instructions = request.Instruction,
            Temperature = request.Settings.Temperature,
            Reasoning = new ReasoningOptions
            {
                Effort = ToReasoningEffort(request.Settings.Thinking),
                Output = ReasoningOutput.None
            },
            ToolMode = ChatToolMode.None
        }.AddOllamaOption(OllamaOption.NumCtx, request.Settings.ContextSize);
        var agent = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                ChatOptions = chatOptions,
                Name = "TextAidRewrite"
            });
        AgentResponse response = await agent.RunAsync(request.InputText, cancellationToken: linkedSource.Token);
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
