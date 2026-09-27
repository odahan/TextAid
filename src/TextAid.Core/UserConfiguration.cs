using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TextAid.Core;

/// <summary>Creates the local configuration on first launch without persisting user text.</summary>
public static class UserConfiguration
{
    public static string EnsureCreated()
    {
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TextAid");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "config.json");
        if (!File.Exists(path))
        {
            var config = new
            {
                schemaVersion = 1,
                strictLocal = true,
                debugMode = false,
                trigger = new { type = "doubleCopy", maximumDelayMs = 450 },
                connections = new[] { new { id = "ollama-local", provider = "ollama", endpoint = "http://127.0.0.1:11434" } },
                profiles = new[] { new { id = "local-default", connectionId = "ollama-local", model = "", temperature = 0.2, timeoutSeconds = 120, providerOptions = new { think = false, num_ctx = 8192 } } }
            };
            File.WriteAllText(path, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }
        return path;
    }

    /// <summary>Returns the persisted visible action folder, preferring the directory beside the executable.</summary>
    public static string EnsureActionsDirectory()
    {
        string configurationPath = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(configurationPath))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        string? persistedDirectory = root["actionsDirectory"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(persistedDirectory) && TryCreateDirectory(persistedDirectory)) return persistedDirectory;

        string primaryDirectory = Path.Combine(AppContext.BaseDirectory, "actions");
        string fallbackDirectory = Path.Combine(Path.GetDirectoryName(configurationPath)!, "actions");
        string selectedDirectory = TryCreateDirectory(primaryDirectory) ? primaryDirectory : fallbackDirectory;
        if (!TryCreateDirectory(selectedDirectory)) throw new InvalidOperationException("TextAid could not create its action directory.");

        root["actionsDirectory"] = selectedDirectory;
        File.WriteAllText(configurationPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return selectedDirectory;
    }

    /// <summary>Loads the first local Ollama profile without retaining user text.</summary>
    public static TextTransformationSettings LoadTransformationSettings()
    {
        string path = EnsureCreated();
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        JsonElement connection = root.GetProperty("connections")[0];
        JsonElement profile = root.GetProperty("profiles")[0];

        string endpoint = connection.GetProperty("endpoint").GetString() ?? string.Empty;
        string model = profile.GetProperty("model").GetString() ?? string.Empty;
        float temperature = profile.GetProperty("temperature").GetSingle();
        int timeoutSeconds = profile.GetProperty("timeoutSeconds").GetInt32();
        int contextSize = profile.TryGetProperty("providerOptions", out JsonElement providerOptions) && providerOptions.TryGetProperty("num_ctx", out JsonElement numCtx)
            ? numCtx.GetInt32()
            : 8192;
        return new TextTransformationSettings(endpoint, model, temperature, TimeSpan.FromSeconds(timeoutSeconds), contextSize, ReadThinkingMode(profile));
    }

    /// <summary>Saves the local Ollama connection and generation options without storing user text.</summary>
    public static void SaveTransformationSettings(TextTransformationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidateLocalSettings(settings);

        string path = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        JsonObject connection = root["connections"]?.AsArray().FirstOrDefault()?.AsObject() ?? throw new InvalidOperationException("The TextAid connection is missing.");
        JsonObject profile = root["profiles"]?.AsArray().FirstOrDefault()?.AsObject() ?? throw new InvalidOperationException("The TextAid profile is missing.");
        JsonObject options = profile["providerOptions"]?.AsObject() ?? new JsonObject();

        connection["endpoint"] = settings.Endpoint;
        profile["model"] = settings.Model;
        profile["temperature"] = settings.Temperature;
        profile["timeoutSeconds"] = (int)settings.Timeout.TotalSeconds;
        options["num_ctx"] = settings.ContextSize;
        options["think"] = settings.Thinking == ThinkingMode.Off ? false : settings.Thinking.ToString().ToLowerInvariant();
        profile["providerOptions"] = options;
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void ValidateLocalSettings(TextTransformationSettings settings)
    {
        if (!Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out Uri? endpoint) || endpoint.Scheme is not "http" and not "https")
            throw new ArgumentException("Enter a valid HTTP Ollama endpoint.", nameof(settings));
        if (!IsLocalHost(endpoint.Host)) throw new ArgumentException("This device only allows localhost, 127.0.0.1, or ::1.", nameof(settings));
        if (settings.Temperature is < 0 or > 2) throw new ArgumentException("Temperature must be between 0 and 2.", nameof(settings));
        if (settings.Timeout <= TimeSpan.Zero) throw new ArgumentException("Timeout must be greater than zero.", nameof(settings));
        if (settings.ContextSize < 512) throw new ArgumentException("Context size must be at least 512 tokens.", nameof(settings));
    }

    private static bool TryCreateDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            string probePath = Path.Combine(directory, $".textaid-write-{Guid.NewGuid():N}.tmp");
            using (File.Create(probePath)) { }
            File.Delete(probePath);
            return true;
        }
        catch (UnauthorizedAccessException) { return false; }
        catch (IOException) { return false; }
    }

    private static bool IsLocalHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        IPAddress.TryParse(host, out IPAddress? address) && IPAddress.IsLoopback(address);

    private static ThinkingMode ReadThinkingMode(JsonElement profile)
    {
        if (!profile.TryGetProperty("providerOptions", out JsonElement options) || !options.TryGetProperty("think", out JsonElement think)) return ThinkingMode.Off;
        return think.ValueKind == JsonValueKind.String && Enum.TryParse(think.GetString(), true, out ThinkingMode mode) ? mode : ThinkingMode.Off;
    }
}
