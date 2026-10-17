using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace TextAid.AI;

/// <summary>Defines the public Sign in with ChatGPT contract, without application credentials.</summary>
public static class OpenAiAccountProtocol
{
    public const string Issuer = "https://auth.openai.com";
    public const string Resource = "https://api.openai.com/v1";
    public const string PlanScope = "chatgpt.tokens.use.direct";
    public const string Scopes = "openid profile email offline_access resource.invoke chatgpt.tokens.use.direct";

    public static string RandomValue() => Encode(RandomNumberGenerator.GetBytes(32));
    public static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static byte[] Decode(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));

    public static string Challenge(string verifier) => Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    public static string AuthorizationUrl(string redirectUri, string hostId, string state, string nonce,
        string verifier, string? clientId = null, string? email = null)
    {
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = clientId ?? "dynamic_agent_client",
            ["ext_agent_host_id"] = hostId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUri,
            ["scope"] = Scopes,
            ["resource"] = Resource,
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge_method"] = "S256",
            ["code_challenge"] = Challenge(verifier)
        };
        if (clientId == null) parameters["agent_name_hint"] = "TextAid";
        if (!string.IsNullOrEmpty(email)) parameters["login_hint"] = email;
        return Issuer + "/api/accounts/authorize?" + string.Join("&",
            parameters.Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)));
    }

    /// <summary>Rejects ambiguous callbacks before any authorization code is exchanged.</summary>
    public static (string Code, string ClientId) ValidateCallback(Uri callback, string redirectUri,
        string state, string? selectedClientId)
    {
        var expected = new Uri(redirectUri);
        if (callback.GetLeftPart(UriPartial.Path) != expected.GetLeftPart(UriPartial.Path) || callback.Fragment.Length != 0)
            throw new OpenAiAccountException("OpenAiAuthorizationFailed");
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in callback.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = item.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            var value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : "";
            if (!parameters.TryAdd(key, value)) throw new OpenAiAccountException("OpenAiAuthorizationFailed");
        }
        if (!parameters.TryGetValue("state", out var actualState) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(actualState)))
            throw new OpenAiAccountException("OpenAiAuthorizationFailed");
        if (parameters.ContainsKey("error")) throw new OpenAiAccountException("OpenAiAuthorizationFailed");
        parameters.TryGetValue("client_id", out var returnedClientId);
        if (selectedClientId != null && returnedClientId != null && returnedClientId != selectedClientId)
            throw new OpenAiAccountException("OpenAiAuthorizationFailed");
        var clientId = selectedClientId ?? returnedClientId;
        if (string.IsNullOrWhiteSpace(clientId) || clientId == "dynamic_agent_client" ||
            !parameters.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
            throw new OpenAiAccountException("OpenAiAuthorizationFailed");
        return (code, clientId);
    }

    public static bool HasPlanScope(string? scope) => (scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(PlanScope);

    /// <summary>Verifies a signed OIDC ID token using keys obtained from the fixed OpenAI JWKS endpoint.</summary>
    public static (string Subject, string Email) ValidateIdToken(string token, JsonObject jwks,
        string clientId, string? nonce, DateTimeOffset now)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token)) throw InvalidIdentity("missing ID token");
            var parts = token.Split('.');
            if (parts.Length != 3) throw InvalidIdentity("invalid ID token format");
            var header = JsonNode.Parse(Decode(parts[0]))!.AsObject();
            if ((string?)header["alg"] != "RS256") throw InvalidIdentity("unsupported signing algorithm");
            var kid = (string?)header["kid"];
            if (string.IsNullOrEmpty(kid)) throw InvalidIdentity("missing signing key identifier");
            var key = jwks["keys"]!.AsArray().OfType<JsonObject>().Single(item => (string?)item["kid"] == kid);
            if ((string?)key["kty"] != "RSA" || (key["use"] != null && (string?)key["use"] != "sig") ||
                (key["alg"] != null && (string?)key["alg"] != "RS256")) throw new FormatException();
            using var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters { Modulus = Decode((string)key["n"]!), Exponent = Decode((string)key["e"]!) });
            if (!rsa.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), Decode(parts[2]),
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) throw InvalidIdentity("signature mismatch");
            var payload = JsonNode.Parse(Decode(parts[1]))!.AsObject();
            var audience = payload["aud"];
            var authorizedParty = (string?)payload["azp"];
            var matchesAudience = audience is JsonArray audiences
                ? audiences.Any(item => (string?)item == clientId) && (audiences.Count == 1 || authorizedParty == clientId)
                : (string?)audience == clientId;
            if ((string?)payload["iss"] != Issuer) throw InvalidIdentity("issuer mismatch");
            if (!matchesAudience || (authorizedParty != null && authorizedParty != clientId)) throw InvalidIdentity("audience mismatch");
            if ((long)payload["exp"]! <= now.ToUnixTimeSeconds()) throw InvalidIdentity("ID token expired");
            if (payload["nbf"] != null && (long)payload["nbf"]! > now.ToUnixTimeSeconds() + 30) throw InvalidIdentity("ID token not yet valid");
            if (nonce != null && (string?)payload["nonce"] != nonce) throw InvalidIdentity("nonce mismatch");
            if (string.IsNullOrWhiteSpace((string?)payload["sub"])) throw InvalidIdentity("missing account identity");
            return ((string)payload["sub"]!, (string?)payload["email"] ?? "ChatGPT account");
        }
        catch (Exception ex) when (ex is not OpenAiAccountException)
        {
            throw new OpenAiAccountException("OpenAiAuthorizationFailed");
        }
    }

    private static OpenAiAccountException InvalidIdentity(string reason) =>
        new("OpenAiAuthorizationFailed");
}

/// <summary>Contains only safe, user-facing diagnostics; never raw OAuth or HTTP response data.</summary>
public sealed class OpenAiAccountException(string message, string? code = null) : Exception(message)
{
    public string? Code { get; } = code;
}
