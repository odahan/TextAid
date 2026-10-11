using System.Net;
using System.Text;
using System.Text.Json;
using TextAid.AI;
using TextAid.Core;

namespace TextAid.AI.Tests;

public sealed class OllamaInferenceTests
{
    [Fact]
    public async Task Inference_StreamsVisibleTextAndMetricsWithoutTheTransportDeadline()
    {
        var handler = new StubHandler(async (request, token) =>
        {
            using JsonDocument json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.True(json.RootElement.GetProperty("stream").GetBoolean());
            Assert.False(json.RootElement.GetProperty("think").GetBoolean());
            Assert.Equal(128, json.RootElement.GetProperty("options").GetProperty("num_predict").GetInt32());
            await Task.Delay(150, token);
            return StreamResponse();
        });
        HttpClient? transport = null;
        var factory = new OllamaChatClientFactory(() => transport = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(25) });
        InferenceDiagnostics? metrics = null;
        var previews = new List<string>();
        var service = new MafTextTransformationService(factory.Create, true, 128, value => metrics = value, previews.Add);

        string result = await service.TransformAsync(Request(TimeSpan.FromSeconds(5)), CancellationToken.None);

        Assert.Equal("Hello world", result);
        Assert.Contains("Hello", previews);
        Assert.Equal(Timeout.InfiniteTimeSpan, transport!.Timeout);
        Assert.True(handler.Disposed);
        Assert.NotNull(metrics);
        Assert.True(metrics.Completed);
        Assert.False(metrics.Cancelled);
        Assert.Equal(20d, metrics.LoadMilliseconds);
        Assert.Equal(30d, metrics.PromptMilliseconds);
        Assert.Equal(40d, metrics.GenerationMilliseconds);
        Assert.Equal(100d, metrics.TotalMilliseconds);
        Assert.Equal(10L, metrics.InputTokens);
        Assert.Equal(2L, metrics.OutputTokens);
        Assert.NotNull(metrics.FirstResponseMilliseconds);
    }

    [Fact]
    public async Task Inference_StillHonorsTheProfileDeadlineAndReportsAnIncompleteRequest()
    {
        var handler = new StubHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return StreamResponse();
        });
        var factory = new OllamaChatClientFactory(() => new HttpClient(handler));
        InferenceDiagnostics? metrics = null;
        var service = new MafTextTransformationService(factory.Create, true, reportDiagnostics: value => metrics = value);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.TransformAsync(Request(TimeSpan.FromMilliseconds(100)), CancellationToken.None));

        Assert.NotNull(metrics);
        Assert.False(metrics.Completed);
        Assert.True(metrics.Cancelled);
        Assert.Null(metrics.TotalMilliseconds);
        Assert.True(handler.Disposed);
    }

    [Fact]
    public async Task Warmup_HonorsTheProfileDeadline()
    {
        var handler = new StubHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return StreamResponse();
        });
        var factory = new OllamaChatClientFactory(() => new HttpClient(handler));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            factory.WarmupAsync(Request(TimeSpan.FromMilliseconds(100)).Settings, CancellationToken.None));
        Assert.True(handler.Disposed);
    }

    [Fact]
    public async Task Warmup_UsesTheProfileContextWithoutOverridingServerRetention()
    {
        var handler = new StubHandler(async (request, token) =>
        {
            using JsonDocument json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.False(json.RootElement.TryGetProperty("keep_alive", out var keepAlive) && keepAlive.ValueKind != JsonValueKind.Null);
            Assert.Equal(string.Empty, json.RootElement.GetProperty("prompt").GetString());
            Assert.Equal(16384, json.RootElement.GetProperty("options").GetProperty("num_ctx").GetInt32());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"model\":\"test-model\",\"response\":\"\",\"done\":true}\n", Encoding.UTF8, "application/x-ndjson")
            };
        });
        var factory = new OllamaChatClientFactory(() => new HttpClient(handler));
        await factory.WarmupAsync(Request(TimeSpan.FromSeconds(5)).Settings with { ContextSize = 16384 }, CancellationToken.None);
    }

    [Fact]
    public async Task ModelState_ReportsMemoryContextAndRetentionAndAcceptsTheDefaultTag()
    {
        var handler = new StubHandler((request, _) =>
        {
            Assert.Equal("/api/ps", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"models":[{"name":"test-model:latest","size":2000,"size_vram":1500,"context_length":16384,"expires_at":"2099-01-01T00:00:00Z"}]}
                    """)
            });
        });
        var factory = new OllamaChatClientFactory(() => new HttpClient(handler));
        OllamaModelState? state = await factory.GetModelStateAsync(Request(TimeSpan.FromSeconds(5)).Settings, CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(16384L, state.ContextSize);
        Assert.Equal(75d, state.GpuMemoryPercent);
        Assert.Equal(2099, state.ExpiresAt!.Value.Year);
    }

    [Fact]
    public async Task Inference_SendsTheActionContextAndThinkingOverride()
    {
        var handler = new StubHandler(async (request, token) =>
        {
            using JsonDocument json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.Equal("low", json.RootElement.GetProperty("think").GetString());
            Assert.Equal(16384, json.RootElement.GetProperty("options").GetProperty("num_ctx").GetInt32());
            return StreamResponse();
        });
        var factory = new OllamaChatClientFactory(() => new HttpClient(handler));
        var action = new ActionDefinition("custom", null, true, "Process {{text}}", "local-default", null, "Unchanged", false,
            Thinking: ThinkingMode.Low, ContextSizeOverride: 16384);
        var request = Request(TimeSpan.FromSeconds(5));
        await new MafTextTransformationService(factory.Create).TransformAsync(
            request with { Settings = action.ApplyInferenceSettings(request.Settings) }, CancellationToken.None);
    }

    private static TextTransformationRequest Request(TimeSpan timeout) => new("Bonjour", "Translate into English.",
        new TextTransformationSettings("http://localhost:11434", "test-model", 0, timeout, 8192, ThinkingMode.Off));

    private static HttpResponseMessage StreamResponse() => new(HttpStatusCode.OK)
    {
        Content = new StringContent("""
            {"model":"test-model","created_at":"2026-10-06T12:00:00Z","message":{"role":"assistant","content":"Hello"},"done":false}
            {"model":"test-model","created_at":"2026-10-06T12:00:00Z","message":{"role":"assistant","content":" world"},"done":false}
            {"model":"test-model","created_at":"2026-10-06T12:00:00Z","message":{"role":"assistant","content":""},"done":true,"done_reason":"stop","total_duration":100000000,"load_duration":20000000,"prompt_eval_duration":30000000,"eval_duration":40000000,"prompt_eval_count":10,"eval_count":2}
            """ + "\n", Encoding.UTF8, "application/x-ndjson")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public bool Disposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
