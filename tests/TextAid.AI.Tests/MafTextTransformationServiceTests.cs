using Microsoft.Extensions.AI;
using TextAid.AI;
using TextAid.Core;

namespace TextAid.AI.Tests;

public sealed class MafTextTransformationServiceTests
{
    [Fact]
    public async Task TransformAsync_SendsOnePromptAndReturnsTheCompleteResponse()
    {
        var client = new FakeChatClient((messages, options, _) =>
        {
            Assert.Contains(messages, message => message.Text == "Original text");
            Assert.Equal("Rewrite clearly.", options?.Instructions);
            Assert.Equal(0.35f, options?.Temperature);
            Assert.Equal(ReasoningEffort.None, options?.Reasoning?.Effort);
            Assert.Equal(ReasoningOutput.None, options?.Reasoning?.Output);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Rewritten text")));
        });
        var service = CreateService(client);

        string result = await service.TransformAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal("Rewritten text", result);
        Assert.Equal(1, client.RequestCount);
    }

    [Fact]
    public async Task TransformAsync_PropagatesCancellation()
    {
        var client = new FakeChatClient(async (_, _, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("The cancellation token was not honored.");
        });
        var service = CreateService(client);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.TransformAsync(CreateRequest(), cancellation.Token));
    }

    [Fact]
    public async Task TransformAsync_PropagatesProviderFailure()
    {
        var client = new FakeChatClient((_, _, _) => throw new HttpRequestException("Ollama is unavailable."));
        var service = CreateService(client);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.TransformAsync(CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task TransformAsync_RejectsAnEmptyResponse()
    {
        var client = new FakeChatClient((_, _, _) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, " "))));
        var service = CreateService(client);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.TransformAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal("The local model returned an empty result.", exception.Message);
    }

    [Fact]
    public async Task TransformAsync_StopsWhenTheConfiguredTimeoutExpires()
    {
        var client = new FakeChatClient(async (_, _, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("The timeout token was not honored.");
        });
        var service = CreateService(client);
        TextTransformationRequest request = CreateRequest() with { Settings = CreateRequest().Settings with { Timeout = TimeSpan.FromMilliseconds(25) } };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.TransformAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task TransformAsync_MapsTheThinkingChoiceToTheChatReasoningEffort()
    {
        var client = new FakeChatClient((_, options, _) =>
        {
            Assert.Equal(ReasoningEffort.High, options?.Reasoning?.Effort);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Rewritten text")));
        });
        var service = CreateService(client);
        TextTransformationRequest request = CreateRequest() with
        {
            Settings = CreateRequest().Settings with { Thinking = ThinkingMode.High }
        };

        await service.TransformAsync(request, CancellationToken.None);
    }

    [Fact]
    public async Task TransformAsync_ReservesHalfOfTheLocalContextForOutput()
    {
        var client = new FakeChatClient((_, options, _) =>
        {
            Assert.Equal(4096, options?.MaxOutputTokens);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Rewritten text")));
        });
        var service = new MafTextTransformationService(_ => client, isOllamaClient: true, maxOutputTokens: 16_384);

        await service.TransformAsync(CreateRequest(), CancellationToken.None);
    }

    [Fact]
    public async Task TransformAsync_UsesTheConfiguredOutputBudgetForAnExternalProvider()
    {
        var client = new FakeChatClient((_, options, _) =>
        {
            Assert.Equal(16_384, options?.MaxOutputTokens);
            Assert.Null(options?.Temperature);
            Assert.Null(options?.Reasoning);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Rewritten text")));
        });
        var service = new MafTextTransformationService(_ => client, isOllamaClient: false, maxOutputTokens: 16_384);

        await service.TransformAsync(CreateRequest(), CancellationToken.None);
    }

    [Fact]
    public async Task TransformAsync_GivesExternalProvidersMoreThanTheLegacyTwoMinuteTimeout()
    {
        var client = new FakeChatClient(async (_, _, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken);
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Rewritten text"));
        });
        var service = new MafTextTransformationService(_ => client, isOllamaClient: false);
        TextTransformationRequest request = CreateRequest() with
        {
            Settings = CreateRequest().Settings with { Timeout = TimeSpan.FromMilliseconds(25) }
        };

        string result = await service.TransformAsync(request, CancellationToken.None);

        Assert.Equal("Rewritten text", result);
    }

    private static MafTextTransformationService CreateService(FakeChatClient client) => new(_ => client);

    private static TextTransformationRequest CreateRequest() => new(
        "Original text",
        "Rewrite clearly.",
        new TextTransformationSettings("http://127.0.0.1:11434", "local-model", 0.35f, TimeSpan.FromSeconds(5), 8192, ThinkingMode.Off));

    private sealed class FakeChatClient(Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>> respond) : IChatClient
    {
        public int RequestCount { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            RequestCount++;
            return respond(messages, options, cancellationToken);
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            RequestCount++;
            ChatResponse response = await respond(messages, options, cancellationToken);
            yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
