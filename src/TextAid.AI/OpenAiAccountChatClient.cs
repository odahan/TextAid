using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace TextAid.AI;

/// <summary>Adapts the official, non-persistent account Responses stream to the MAF chat boundary.</summary>
public sealed class OpenAiAccountChatClient(HttpClient http, OpenAiAccountSession session, string model) : IChatClient
{
    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var text = new StringBuilder();
        await foreach (var update in GetStreamingResponseAsync(messages, options, cancellationToken)) text.Append(update.Text);
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, text.ToString()));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (options?.Temperature is not null || options?.MaxOutputTokens is not null || options?.TopP is not null || options?.Tools?.Count > 0 || options?.AdditionalProperties?.Count > 0)
            throw new OpenAiAccountException("OpenAiUnsupportedOptions");
        var input = new JsonArray();
        var instructions = new StringBuilder(options?.Instructions ?? "");
        foreach (ChatMessage message in messages)
        {
            if (message.Contents.Any(content => content is not TextContent)) throw new OpenAiAccountException("OpenAiUnsupportedOptions");
            if (message.Role == ChatRole.System || message.Role.Value == "developer") instructions.AppendLine().Append(message.Text);
            else if (message.Role == ChatRole.User || message.Role == ChatRole.Assistant)
                input.Add(new JsonObject { ["role"] = message.Role.Value, ["content"] = message.Text });
            else throw new OpenAiAccountException("OpenAiUnsupportedOptions");
        }
        var body = new JsonObject { ["model"] = model, ["input"] = input, ["instructions"] = instructions.ToString(), ["store"] = false, ["stream"] = true };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses") { Content = JsonContent.Create(body) };
        string accessToken = await session.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            await session.RequireReconnectAsync(accessToken);
        if (!response.IsSuccessStatusCode)
            throw new OpenAiAccountException(response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => "OpenAiReconnect",
                System.Net.HttpStatusCode.TooManyRequests => "OpenAiQuotaFailed",
                _ => "OpenAiRequestFailed"
            });
        string? mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null && mediaType != "text/event-stream") throw new OpenAiAccountException("OpenAiStreamFailed");
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var accumulated = new StringBuilder();
        var data = new StringBuilder();
        bool completed = false;
        string? responseId = null;
        while (await reader.ReadLineAsync(cancellationToken) is string line)
        {
            if (line.StartsWith("data:", StringComparison.Ordinal)) { data.AppendLine(line[5..].TrimStart()); continue; }
            if (line.Length != 0 || data.Length == 0) continue;
            string payload = data.ToString().Trim();
            data.Clear();
            if (payload == "[DONE]") break;
            JsonObject item;
            try { item = JsonNode.Parse(payload)!.AsObject(); }
            catch { throw new OpenAiAccountException("OpenAiStreamFailed"); }
            string? type = (string?)item["type"];
            if ((string?)item["part"]?["type"] == "refusal" ||
                (item["item"]?["content"] as JsonArray)?.OfType<JsonObject>().Any(content => (string?)content["type"] == "refusal") == true)
                throw new OpenAiAccountException("OpenAiStreamFailed");
            if (completed) throw new OpenAiAccountException("OpenAiStreamFailed");
            if (type is "response.refusal.delta" or "response.refusal.done" or "response.failed" or "response.incomplete" or "error")
                throw new OpenAiAccountException("OpenAiStreamFailed");
            if (type == "response.output_text.delta")
            {
                string delta = (string?)item["delta"] ?? throw new OpenAiAccountException("OpenAiStreamFailed");
                accumulated.Append(delta);
                yield return new ChatResponseUpdate(ChatRole.Assistant, delta) { ModelId = model };
            }
            if (type == "response.completed")
            {
                JsonObject terminal = item["response"]?.AsObject() ?? throw new OpenAiAccountException("OpenAiStreamFailed");
                if ((string?)terminal["status"] != "completed" || terminal["error"] is not null || terminal["incomplete_details"] is not null)
                    throw new OpenAiAccountException("OpenAiStreamFailed");
                var final = new StringBuilder();
                foreach (JsonObject output in (terminal["output"] as JsonArray ?? []).OfType<JsonObject>())
                {
                    foreach (JsonObject content in (output["content"] as JsonArray ?? []).OfType<JsonObject>())
                    {
                        if ((string?)content["type"] == "refusal") throw new OpenAiAccountException("OpenAiStreamFailed");
                        if ((string?)content["type"] == "output_text") final.Append((string?)content["text"]);
                    }
                }
                if (final.Length > 0 && accumulated.Length > 0 && final.ToString() != accumulated.ToString()) throw new OpenAiAccountException("OpenAiStreamFailed");
                if (accumulated.Length == 0 && final.Length > 0)
                {
                    accumulated.Append(final);
                    yield return new ChatResponseUpdate(ChatRole.Assistant, final.ToString()) { ModelId = model };
                }
                if (string.IsNullOrWhiteSpace(accumulated.ToString())) throw new OpenAiAccountException("OpenAiStreamFailed");
                responseId = (string?)terminal["id"];
                completed = true;
                // Terminal success is sufficient; closing the HTTP response cannot turn it into a retry.
                break;
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (!completed) throw new OpenAiAccountException("OpenAiStreamFailed");
        yield return new ChatResponseUpdate { Role = ChatRole.Assistant, ModelId = model, ResponseId = responseId, FinishReason = ChatFinishReason.Stop };
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType == typeof(IChatClient) ? this : null;
    /// <inheritdoc />
    public void Dispose() => http.Dispose();
}
