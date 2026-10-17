using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using TextAid.Core;

namespace TextAid.AI.Tests;

public sealed class OpenAiAccountTests
{
    [Fact]
    public void Pkce_UsesS256RfcVectorAndTextAidIdentity()
    {
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", OpenAiAccountProtocol.Challenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));
        string url = OpenAiAccountProtocol.AuthorizationUrl("http://127.0.0.1:1234/auth/callback", "host", "state", "nonce", "verifier");
        Assert.Contains("agent_name_hint=TextAid", url);
        Assert.Contains("code_challenge_method=S256", url);
        Assert.DoesNotContain("client_secret", url);
        Assert.NotEqual(OpenAiAccountProtocol.RandomValue(), OpenAiAccountProtocol.RandomValue());
    }

    [Theory]
    [InlineData("/wrong?state=state&code=code&client_id=oaiapp_test")]
    [InlineData("/auth/callback?state=wrong&code=code&client_id=oaiapp_test")]
    [InlineData("/auth/callback?state=state&state=state&code=code&client_id=oaiapp_test")]
    [InlineData("/auth/callback?state=state&code=code&client_id=wrong")]
    [InlineData("/auth/callback?state=state&client_id=oaiapp_test")]
    [InlineData("/auth/callback?state=state&code=code&client_id=oaiapp_test#fragment")]
    [InlineData("/auth/callback?state=state&error=access_denied")]
    public void Callback_RejectsUntrustedOrAmbiguousValues(string path)
    {
        Assert.Throws<OpenAiAccountException>(() => OpenAiAccountProtocol.ValidateCallback(new Uri("http://127.0.0.1:1234" + path), "http://127.0.0.1:1234/auth/callback", "state", "oaiapp_test"));
    }

    [Fact]
    public async Task Loopback_IsCancellableAndDisposedPortCannotAcceptReplay()
    {
        string redirect;
        using (var callback = new OpenAiLoopbackCallback())
        {
            redirect = callback.RedirectUri;
            Assert.Equal("127.0.0.1", new Uri(redirect).Host);
            using var cancellation = new CancellationTokenSource();
            Task<Uri> pending = callback.ReceiveAsync(cancellation.Token);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        }
        using var client = new System.Net.Sockets.TcpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Exception? failure = await Record.ExceptionAsync(() => client.ConnectAsync("127.0.0.1", new Uri(redirect).Port, timeout.Token).AsTask());
        Assert.True(failure is System.Net.Sockets.SocketException or OperationCanceledException);
        Assert.False(client.Connected);
    }

    [Theory]
    [InlineData("nonce")]
    [InlineData("iss")]
    [InlineData("aud")]
    [InlineData("azp")]
    [InlineData("exp")]
    [InlineData("signature")]
    public void Identity_RequiresSignedIssuerAudienceExpiryAndNonce(string invalid)
    {
        using RSA key = RSA.Create(2048);
        JsonObject payload = IdentityPayload("nonce");
        JsonObject keys = Keys(key);
        if (invalid == "exp") payload[invalid] = 1;
        else if (invalid != "signature") payload[invalid] = "wrong";
        string signed = Sign(key, payload);
        if (invalid == "signature") signed = signed[..signed.LastIndexOf('.')] + "." + OpenAiAccountProtocol.Encode(new byte[256]);
        Assert.Throws<OpenAiAccountException>(() => OpenAiAccountProtocol.ValidateIdToken(signed, keys, "oaiapp_test", "nonce", DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task SignIn_ValidatesAndPersistsOwnRegistration_AndFailedReplacementPreservesSession()
    {
        using RSA key = RSA.Create(2048);
        var vault = new MemoryVault();
        string? nonce = null;
        string? redirect = null;
        bool reject = false;
        var handler = new Handler(async request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("jwks.json")) return Json(Keys(key));
            string form = await request.Content!.ReadAsStringAsync();
            Assert.Contains("client_id=oaiapp_test", form);
            Assert.Contains("redirect_uri=" + Uri.EscapeDataString(redirect!), form);
            Assert.Contains("code_verifier=", form);
            var tokens = Tokens();
            tokens["id_token"] = Sign(key, IdentityPayload(reject ? "wrong" : nonce!));
            return Json(tokens);
        });
        using var http = new HttpClient(handler);
        var service = new OpenAiAccountSession(http, vault, openBrowser: async url =>
        {
            Dictionary<string, string> values = Query(new Uri(url));
            Assert.Equal("TextAid", values.GetValueOrDefault("agent_name_hint", "TextAid"));
            nonce = values["nonce"];
            redirect = values["redirect_uri"];
            using var browser = new HttpClient();
            using var returned = await browser.GetAsync(redirect + "?state=" + values["state"] + "&code=synthetic&client_id=oaiapp_test");
            Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        });
        await service.SignInAsync(null, CancellationToken.None);
        string saved = vault.Value!;
        Assert.Equal("OpenAiConnected", service.StatusKey);
        Assert.Equal("oaiapp_test", (string?)JsonNode.Parse(saved)!["client_id"]);
        reject = true;
        await Assert.ThrowsAsync<OpenAiAccountException>(() => service.SignInAsync(null, CancellationToken.None));
        Assert.Equal(saved, vault.Value);
    }

    [Fact]
    public async Task Refresh_IsSerializedAndRotatedTokenIsPersistedBeforeReuse()
    {
        var vault = new MemoryVault { Value = Saved(expired: true) };
        int refreshes = 0;
        using var http = new HttpClient(new Handler(async request =>
        {
            Interlocked.Increment(ref refreshes);
            Assert.Contains("refresh_token=refresh", await request.Content!.ReadAsStringAsync());
            await Task.Delay(30);
            var tokens = Tokens();
            tokens["refresh_token"] = "rotated";
            return Json(tokens);
        }));
        var service = new OpenAiAccountSession(http, vault);
        await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => service.GetAccessTokenAsync(CancellationToken.None)));
        Assert.Equal(1, refreshes);
        Assert.Contains("rotated", vault.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Refresh_TerminalRevocationClearsTokens_TransientFailurePreservesThem(bool terminal)
    {
        var vault = new MemoryVault { Value = Saved(expired: true) };
        string previous = vault.Value;
        using var http = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage(terminal ? HttpStatusCode.BadRequest : HttpStatusCode.ServiceUnavailable)
        { Content = new StringContent(terminal ? "{\"error\":\"invalid_grant\"}" : "{}") })));
        var service = new OpenAiAccountSession(http, vault);
        await Assert.ThrowsAsync<OpenAiAccountException>(() => service.GetAccessTokenAsync(CancellationToken.None));
        if (terminal) Assert.DoesNotContain("access_token", vault.Value);
        else Assert.Equal(previous, vault.Value);
    }

    [Fact]
    public async Task SignOut_ClearsLocalTokensDespiteRemoteFailure()
    {
        var vault = new MemoryVault { Value = Saved() };
        using var http = new HttpClient(new Handler(_ => throw new HttpRequestException("synthetic network failure")));
        Assert.False(await new OpenAiAccountSession(http, vault).SignOutAsync(CancellationToken.None));
        Assert.DoesNotContain("access_token", vault.Value);
        Assert.DoesNotContain("refresh_token", vault.Value);
        Assert.Contains("oaiapp_test", vault.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Responses_MapsMafMessagesAndRequiresTerminalSuccess(bool missingContentType)
    {
        var vault = new MemoryVault { Value = Saved() };
        int calls = 0;
        using var http = new HttpClient(new Handler(async request =>
        {
            calls++;
            Assert.Equal("https://api.openai.com/v1/responses", request.RequestUri!.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync())!;
            Assert.False((bool)body["store"]!);
            Assert.True((bool)body["stream"]!);
            Assert.Null(body["temperature"]);
            Assert.Null(body["max_output_tokens"]);
            Assert.Contains("Rewrite", (string?)body["instructions"]);
            Assert.Equal("input", (string?)body["input"]![0]!["content"]);
            var response = Sse(Delta + Completed);
            if (missingContentType) response.Content.Headers.ContentType = null;
            return response;
        }));
        var client = new OpenAiAccountChatClient(http, new OpenAiAccountSession(http, vault), "offered");
        var service = new MafTextTransformationService(_ => client, false);
        string result = await service.TransformAsync(new("input", "Rewrite", new(UserConfiguration.DefaultExternalEndpoint, "offered", 0.2f, TimeSpan.FromMinutes(1), 8192, ThinkingMode.Off)), CancellationToken.None);
        Assert.Equal("Hello", result);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("data: {\"type\":\"response.output_text.delta\",\"delta\":\"partial\"}\n\n")]
    [InlineData("data: {\"type\":\"response.refusal.done\"}\n\n")]
    [InlineData("data: {\"type\":\"response.incomplete\"}\n\n")]
    [InlineData("data: malformed\n\n")]
    [InlineData("data: [DONE]\n\n")]
    [InlineData("data: {\"type\":\"response.content_part.added\",\"part\":{\"type\":\"refusal\"}}\n\n")]
    public async Task Responses_RejectsPartialRefusedMalformedOrIncompleteWithoutRetry(string stream)
    {
        int calls = 0;
        using var http = new HttpClient(new Handler(_ => { calls++; return Task.FromResult(Sse(stream)); }));
        var client = new OpenAiAccountChatClient(http, new OpenAiAccountSession(http, new MemoryVault { Value = Saved() }), "offered");
        await Assert.ThrowsAsync<OpenAiAccountException>(() => client.GetResponseAsync([new(ChatRole.User, "input")]));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ModelDiscovery_UsesOfferedSlugsAndDoesNotGenerate()
    {
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal("/v1/models", request.RequestUri!.AbsolutePath);
            return Task.FromResult(Json(JsonNode.Parse("{\"models\":[{\"slug\":\"offered\",\"display_name\":\"Offered model\",\"visibility\":\"list\"},{\"slug\":\"hidden\",\"visibility\":\"hide\"}]}")!.AsObject()));
        }));
        var models = await new OpenAiAccountSession(http, new MemoryVault { Value = Saved() }).GetModelsAsync(CancellationToken.None);
        Assert.Equal("offered", Assert.Single(models).Slug);
    }

    [Fact]
    public async Task MissingPlanPermission_PreventsAllInferenceHttpRequests()
    {
        var json = JsonNode.Parse(Saved())!;
        json["scope"] = "openid profile email";
        using var http = new HttpClient(new Handler(_ => throw new InvalidOperationException("No HTTP should run.")));
        var session = new OpenAiAccountSession(http, new MemoryVault { Value = json.ToJsonString() });
        Assert.Equal("OpenAiPermissionMissing", session.StatusKey);
        await Assert.ThrowsAsync<OpenAiAccountException>(() => session.GetAccessTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Responses_RejectsConflictingFinalText()
    {
        string stream = Delta + "data: {\"type\":\"response.completed\",\"response\":{\"status\":\"completed\",\"output\":[{\"content\":[{\"type\":\"output_text\",\"text\":\"Different\"}]}]}}\n\n";
        using var http = new HttpClient(new Handler(_ => Task.FromResult(Sse(stream))));
        var client = new OpenAiAccountChatClient(http, new OpenAiAccountSession(http, new MemoryVault { Value = Saved() }), "offered");
        await Assert.ThrowsAsync<OpenAiAccountException>(() => client.GetResponseAsync([new(ChatRole.User, "input")]));
    }

    [Fact]
    public async Task UnsupportedOptions_FailBeforeNetworkOrRefresh()
    {
        using var http = new HttpClient(new Handler(_ => throw new InvalidOperationException("No HTTP should run.")));
        var client = new OpenAiAccountChatClient(http, new OpenAiAccountSession(http, new MemoryVault()), "offered");
        await Assert.ThrowsAsync<OpenAiAccountException>(() => client.GetResponseAsync([new(ChatRole.User, "input")], new() { Temperature = 0.2f }));
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(429)]
    public async Task RejectedInference_DoesNotRetryAndDisablesRevokedPermissionOnly(int status)
    {
        var vault = new MemoryVault { Value = Saved() };
        int calls = 0;
        using var http = new HttpClient(new Handler(_ => { calls++; return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)); }));
        var client = new OpenAiAccountChatClient(http, new OpenAiAccountSession(http, vault), "offered");
        await Assert.ThrowsAsync<OpenAiAccountException>(() => client.GetResponseAsync([new(ChatRole.User, "input")]));
        Assert.Equal(1, calls);
        Assert.Equal(status == 429, JsonNode.Parse(vault.Value!)!["access_token"] is not null);
    }

    [Fact]
    public void Factory_RejectsArbitraryAccountEndpoint()
    {
        var connection = new ConnectionDefinition("account", ConnectionCategory.External, "openai-account", "https://example.com/v1", true, AuthenticationKind.OpenAiAccount, "session");
        var profile = new ModelProfile("account", "account", "offered", 0.2f, TimeSpan.FromMinutes(1), new Dictionary<string, object?>());
        Assert.Throws<OpenAiAccountException>(() => new AiClientFactory().Create(connection, profile));
    }

    [Fact]
    public async Task AccountMafTimeout_IsHonoredWithoutGenerationRetry()
    {
        var handler = new WaitingHandler();
        using var http = new HttpClient(handler);
        var client = new OpenAiAccountChatClient(http, new OpenAiAccountSession(http, new MemoryVault { Value = Saved() }), "offered");
        var service = new MafTextTransformationService(_ => client, false, honorConfiguredTimeout: true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.TransformAsync(
            new("input", "Rewrite", new(UserConfiguration.DefaultExternalEndpoint, "offered", 0.2f, TimeSpan.FromMilliseconds(30), 8192, ThinkingMode.Off)), CancellationToken.None));
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public void GenericFactory_RejectsAccountSessionReferenceBeforeNetwork()
    {
        var connection = new ConnectionDefinition("key", ConnectionCategory.External, "openai-compatible", "https://example.com/v1", true, AuthenticationKind.ApiKey, "openai-account-session");
        var profile = new ModelProfile("key", "key", "model", 0.2f, TimeSpan.FromMinutes(1), new Dictionary<string, object?>());
        Assert.Throws<InvalidOperationException>(() => new OpenAiCompatibleChatClientFactory(new MemoryVault { Value = Saved() }).Create(connection, profile));
    }

    [Fact]
    public async Task SignIn_CancellationPreservesPreviouslyValidCredentials()
    {
        var vault = new MemoryVault { Value = Saved() };
        using var cancellation = new CancellationTokenSource();
        using var http = new HttpClient(new Handler(_ => throw new InvalidOperationException("No exchange expected.")));
        var service = new OpenAiAccountSession(http, vault, openBrowser: _ => { cancellation.Cancel(); return Task.CompletedTask; });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SignInAsync(null, cancellation.Token));
        Assert.Equal("synthetic", (string?)JsonNode.Parse(vault.Value!)!["access_token"]);
    }

    [Fact]
    public async Task Refresh_ReadBackFailure_RestoresPreviousProtectedRecord()
    {
        var vault = new ReadBackFailureVault(Saved(expired: true));
        using var http = new HttpClient(new Handler(_ => Task.FromResult(Json(Tokens()))));
        await Assert.ThrowsAsync<OpenAiAccountException>(() => new OpenAiAccountSession(http, vault).GetAccessTokenAsync(CancellationToken.None));
        Assert.Equal(vault.Original, vault.Value);
    }

    [Fact]
    public void ProtectedSession_UsesIsolatedCurrentUserDpapiAndNeverWritesPlaintext()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TextAid-account-" + Guid.NewGuid().ToString("N"));
        try
        {
            var vault = new DpapiSecretVault(directory);
            string saved = Saved();
            vault.SetSecret("isolated-account", saved);
            Assert.True(vault.TryGetSecret("isolated-account", out string verified));
            Assert.Equal(saved, verified);
            byte[] bytes = File.ReadAllBytes(Assert.Single(Directory.GetFiles(directory)));
            Assert.DoesNotContain("synthetic", Encoding.UTF8.GetString(bytes));
            vault.RemoveSecret("isolated-account");
            Assert.False(vault.TryGetSecret("isolated-account", out _));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OAuthValidationFailure_NeverIncludesCredentialValuesInDiagnostics(bool full)
    {
        string directory = Path.Combine(Path.GetTempPath(), "TextAid-log-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var log = DebugSessionLog.Start(true, full, directory))
            {
                Exception error = Assert.Throws<OpenAiAccountException>(() => OpenAiAccountProtocol.ValidateCallback(
                    new Uri("http://127.0.0.1:1234/auth/callback?state=wrong&code=sentinel-code&client_id=sentinel-client"),
                    "http://127.0.0.1:1234/auth/callback", "sentinel-state", null));
                log.WriteException("openai-signin", error);
            }
            string content = File.ReadAllText(Path.Combine(directory, "TextAid.debug.log"));
            Assert.DoesNotContain("sentinel", content);
            Assert.DoesNotContain("127.0.0.1", content);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private sealed class ReadBackFailureVault(string original) : ISecretVault
    {
        public string Original { get; } = original;
        public string Value { get; private set; } = original;
        private bool failRead;
        public void SetSecret(string reference, string secret) { Value = secret; failRead = secret != Original; }
        public bool TryGetSecret(string reference, out string secret) { secret = Value; return !failRead; }
        public void RemoveSecret(string reference) => Value = "";
    }

    private const string Delta = "data: {\"type\":\"response.output_text.delta\",\"delta\":\"Hello\"}\n\n";
    private const string Completed = "data: {\"type\":\"response.completed\",\"response\":{\"id\":\"response\",\"status\":\"completed\",\"output\":[]}}\n\n";
    private static HttpResponseMessage Sse(string stream) => new(HttpStatusCode.OK) { Content = new StringContent(stream, Encoding.UTF8, "text/event-stream") };
    private static HttpResponseMessage Json(JsonObject value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    private static JsonObject Tokens() => new() { ["access_token"] = "synthetic", ["refresh_token"] = "refresh", ["scope"] = OpenAiAccountProtocol.Scopes, ["token_type"] = "Bearer", ["expires_in"] = 3600 };
    private static string Saved(bool expired = false)
    {
        var value = Tokens();
        value["client_id"] = "oaiapp_test";
        value["expires_at"] = expired ? 1 : DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        return value.ToJsonString();
    }
    private static JsonObject IdentityPayload(string nonce) => new() { ["iss"] = OpenAiAccountProtocol.Issuer, ["aud"] = "oaiapp_test", ["sub"] = "synthetic-user", ["nonce"] = nonce, ["exp"] = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds() };
    private static JsonObject Keys(RSA rsa)
    {
        RSAParameters key = rsa.ExportParameters(false);
        return new() { ["keys"] = new JsonArray(new JsonObject { ["kid"] = "test", ["kty"] = "RSA", ["n"] = OpenAiAccountProtocol.Encode(key.Modulus!), ["e"] = OpenAiAccountProtocol.Encode(key.Exponent!) }) };
    }
    private static string Sign(RSA key, JsonObject payload)
    {
        string value = OpenAiAccountProtocol.Encode(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"kid\":\"test\"}")) + "." + OpenAiAccountProtocol.Encode(Encoding.UTF8.GetBytes(payload.ToJsonString()));
        return value + "." + OpenAiAccountProtocol.Encode(key.SignData(Encoding.ASCII.GetBytes(value), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }
    private static Dictionary<string, string> Query(Uri url) => url.Query.TrimStart('?').Split('&').Select(pair => pair.Split('=', 2)).ToDictionary(pair => Uri.UnescapeDataString(pair[0]), pair => Uri.UnescapeDataString(pair[1]));
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
    private sealed class WaitingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            throw new InvalidOperationException();
        }
    }
    private sealed class MemoryVault : ISecretVault
    {
        public string? Value { get; set; }
        public void SetSecret(string reference, string secret) => Value = secret;
        public bool TryGetSecret(string reference, out string secret) { secret = Value ?? ""; return Value is not null; }
        public void RemoveSecret(string reference) => Value = null;
    }
}
