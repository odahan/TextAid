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
