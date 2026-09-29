using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TextAid.Core;

/// <summary>Creates the local configuration on first launch without persisting user text.</summary>
public static class UserConfiguration
{
    /// <summary>Provides the prefilled endpoint for the standard OpenAI-compatible External connection.</summary>
    public const string DefaultExternalEndpoint = "https://api.openai.com/v1";
    private static ConfigurationSnapshot? activeConfiguration;
    public static string EnsureCreated()
    {
        string directory = GetUserDataDirectory();
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "config.json");
        if (!File.Exists(path))
        {
            var config = new
            {
                schemaVersion = 1,
                debugMode = false,
                fullDebugMode = false,
                userLanguage = "en",
                preferredTranslationLanguage = "fr",
                shortcuts = new { normalAction = "Ctrl+C+C", quickTranslation = "Ctrl+C+T" },
                actionPresets = new[] { "correct", "rewrite", "summarize", "translate" },
                connections = new object[]
                {
                    new { id = "ollama-local", category = "ThisDeviceOnly", provider = "ollama", endpoint = "http://127.0.0.1:11434", isEnabled = true, authentication = "None", secretReference = (string?)null },
                    new { id = "ollama-network", category = "OnPremises", provider = "ollama", endpoint = "", isEnabled = false, authentication = "None", secretReference = (string?)null },
                    new { id = "openai-external", category = "External", provider = "openai-compatible", endpoint = DefaultExternalEndpoint, isEnabled = false, authentication = "ApiKey", secretReference = "external-api-key" }
                },
                profiles = new object[]
                {
                    new { id = "local-default", connectionId = "ollama-local", model = "", temperature = 0.2, timeoutSeconds = 120, providerOptions = new { think = false, num_ctx = 8192 } },
                    new { id = "network-default", connectionId = "ollama-network", model = "", temperature = 0.2, timeoutSeconds = 120, providerOptions = new { } },
                    new { id = "external-default", connectionId = "openai-external", model = "", temperature = 0.2, timeoutSeconds = 120, providerOptions = new { } }
                }
            };
            File.WriteAllText(path, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }
        else UpgradeVersionOneConfiguration(path);
        return path;
    }

    /// <summary>Returns whether the user explicitly enabled the diagnostic log.</summary>
    public static bool LoadDebugMode()
    {
        string path = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        return root["debugMode"]?.GetValue<bool>() ?? false;
    }

    /// <summary>Persists the opt-in diagnostic setting without creating a log itself.</summary>
    public static void SaveDebugMode(bool enabled)
    {
        string path = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        root["debugMode"] = enabled;
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Returns whether the user enabled the persistent full diagnostic log.</summary>
    public static bool LoadFullDebugMode()
    {
        string path = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        return root["fullDebugMode"]?.GetValue<bool>() ?? false;
    }

    /// <summary>Persists the full diagnostic preference independently of the Debug on/off switch.</summary>
    public static void SaveFullDebugMode(bool enabled)
    {
        string path = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        root["fullDebugMode"] = enabled;
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
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

    /// <summary>Loads and validates the complete versioned connection and profile configuration.</summary>
    public static ConfigurationSnapshot LoadConfiguration()
    {
        try
        {
            ConfigurationSnapshot configuration = LoadConfigurationCore();
            activeConfiguration = configuration;
            return configuration;
        }
        catch when (activeConfiguration is not null)
        {
            return activeConfiguration;
        }
    }

    /// <summary>Reloads configuration atomically and reports an invalid persisted file without replacing the active snapshot.</summary>
    public static ConfigurationSnapshot ReloadConfiguration()
    {
        ConfigurationSnapshot configuration = LoadConfigurationCore();
        activeConfiguration = configuration;
        return configuration;
    }

    private static ConfigurationSnapshot LoadConfigurationCore()
    {
        string path = EnsureCreated();
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        int schemaVersion = root.GetProperty("schemaVersion").GetInt32();
        if (schemaVersion != 1) throw new InvalidOperationException("The TextAid configuration version is not supported.");

        var connections = new List<ConnectionDefinition>();
        foreach (JsonElement item in root.GetProperty("connections").EnumerateArray())
        {
            string id = item.GetProperty("id").GetString() ?? string.Empty;
            ConnectionCategory category = ReadEnum(item, "category", id.Equals("ollama-local", StringComparison.OrdinalIgnoreCase) ? ConnectionCategory.ThisDeviceOnly : ConnectionCategory.OnPremises);
            AuthenticationKind authentication = ReadEnum(item, "authentication", AuthenticationKind.None);
            connections.Add(new ConnectionDefinition(
                id,
                category,
                item.GetProperty("provider").GetString() ?? string.Empty,
                item.GetProperty("endpoint").GetString() ?? string.Empty,
                item.TryGetProperty("isEnabled", out JsonElement isEnabled) ? isEnabled.GetBoolean() : category == ConnectionCategory.ThisDeviceOnly,
                authentication,
                item.TryGetProperty("secretReference", out JsonElement secretReference) ? secretReference.GetString() : null));
        }

        var profiles = new List<ModelProfile>();
        foreach (JsonElement item in root.GetProperty("profiles").EnumerateArray())
        {
            var options = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (item.TryGetProperty("providerOptions", out JsonElement providerOptions) && providerOptions.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty option in providerOptions.EnumerateObject()) options[option.Name] = option.Value.ToString();
            }

            profiles.Add(new ModelProfile(
                item.GetProperty("id").GetString() ?? string.Empty,
                item.GetProperty("connectionId").GetString() ?? string.Empty,
                item.GetProperty("model").GetString() ?? string.Empty,
                item.GetProperty("temperature").GetSingle(),
                TimeSpan.FromSeconds(item.GetProperty("timeoutSeconds").GetInt32()),
                options));
        }

        string userLanguage = root.TryGetProperty("userLanguage", out JsonElement userLanguageElement) ? userLanguageElement.GetString() ?? "en" : "en";
        string preferredLanguage = root.TryGetProperty("preferredTranslationLanguage", out JsonElement preferredLanguageElement) ? preferredLanguageElement.GetString() ?? "fr" : "fr";
        string normalShortcut = root.TryGetProperty("shortcuts", out JsonElement shortcuts) && shortcuts.TryGetProperty("normalAction", out JsonElement normal) ? normal.GetString() ?? "Ctrl+C+C" : "Ctrl+C+C";
        string translationShortcut = root.TryGetProperty("shortcuts", out shortcuts) && shortcuts.TryGetProperty("quickTranslation", out JsonElement translation) ? translation.GetString() ?? "Ctrl+C+T" : "Ctrl+C+T";
        var configuration = new ConfigurationSnapshot(schemaVersion, connections, profiles, userLanguage, preferredLanguage, normalShortcut, translationShortcut);
        ValidateConfiguration(configuration);
        return configuration;
    }

    /// <summary>Validates the cross-reference and user-preference invariants of a configuration snapshot.</summary>
    public static void ValidateConfiguration(ConfigurationSnapshot configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        EnsureUnique(configuration.Connections.Select(connection => connection.Id), "connection");
        EnsureUnique(configuration.Profiles.Select(profile => profile.Id), "profile");
        if (!configuration.Profiles.Any(profile => profile.Id.Equals("local-default", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("The local default profile is missing.");
        foreach (ModelProfile profile in configuration.Profiles)
        {
            if (!configuration.Connections.Any(connection => connection.Id.Equals(profile.ConnectionId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Profile '{profile.Id}' references an unknown connection.");
            if (profile.Temperature is < 0 or > 2 || profile.Timeout <= TimeSpan.Zero)
                throw new InvalidOperationException($"Profile '{profile.Id}' has invalid generation settings.");
        }

        ValidateLanguage(configuration.UserLanguage, "user language");
        ValidateLanguage(configuration.PreferredTranslationLanguage, "preferred translation language");
        if (configuration.UserLanguage.Equals(configuration.PreferredTranslationLanguage, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The user and preferred translation languages must be distinct.");
        if (!IsSupportedShortcut(configuration.NormalShortcut) || !IsSupportedShortcut(configuration.TranslationShortcut) || configuration.NormalShortcut.Equals(configuration.TranslationShortcut, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The configured shortcuts must be valid and distinct.");
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

    /// <summary>Saves remote endpoints while placing supplied credentials in the per-user DPAPI vault.</summary>
    public static void SaveRemoteConnectionSettings(
        string networkEndpoint,
        string networkModel,
        string? networkSecret,
        string externalEndpoint,
        string externalModel,
        string? externalSecret,
        bool saveNetworkConnection = true,
        bool saveExternalConnection = true)
    {
        if (saveNetworkConnection) ValidateOptionalEndpoint(networkEndpoint, ConnectionCategory.OnPremises);
        if (saveExternalConnection) ValidateOptionalEndpoint(externalEndpoint, ConnectionCategory.External);
        string path = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        JsonArray connections = root["connections"]?.AsArray() ?? throw new InvalidOperationException("The TextAid connections are missing.");
        JsonArray profiles = root["profiles"]?.AsArray() ?? throw new InvalidOperationException("The TextAid profiles are missing.");
        EnsureRemoteConnectionDefinitions(connections, profiles);
        if (saveNetworkConnection)
            SaveRemoteConnection(connections, profiles, "ollama-network", "network-default", networkEndpoint, networkModel, networkSecret, "network-api-key");
        if (saveExternalConnection)
            SaveRemoteConnection(connections, profiles, "openai-external", "external-default", externalEndpoint, externalModel, externalSecret, "external-api-key");
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Persists whether each independently configured connection may participate in resolution.</summary>
    public static void SaveConnectionActivation(bool localEnabled, bool networkEnabled, bool externalEnabled)
    {
        string path = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        JsonArray connections = root["connections"]?.AsArray() ?? throw new InvalidOperationException("The TextAid connections are missing.");
        JsonArray profiles = root["profiles"]?.AsArray() ?? throw new InvalidOperationException("The TextAid profiles are missing.");
        EnsureRemoteConnectionDefinitions(connections, profiles);
        SetConnectionActivation(connections, "ollama-local", localEnabled);
        SetConnectionActivation(connections, "ollama-network", networkEnabled);
        SetConnectionActivation(connections, "openai-external", externalEnabled);
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Persists the distinct user-language and keyboard-sequence preferences.</summary>
    public static void SaveUserPreferences(string userLanguage, string preferredTranslationLanguage, string normalShortcut, string translationShortcut)
    {
        var proposed = new ConfigurationSnapshot(1, [
            new ConnectionDefinition("validation-local", ConnectionCategory.ThisDeviceOnly, "ollama", "http://127.0.0.1:11434", false, AuthenticationKind.None, null)],
            [new ModelProfile("local-default", "validation-local", string.Empty, 0.2f, TimeSpan.FromSeconds(120), new Dictionary<string, object?>())],
            userLanguage, preferredTranslationLanguage, normalShortcut, translationShortcut);
        ValidateConfiguration(proposed);

        string path = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        root["userLanguage"] = userLanguage;
        root["preferredTranslationLanguage"] = preferredTranslationLanguage;
        root["shortcuts"] = new JsonObject { ["normalAction"] = normalShortcut, ["quickTranslation"] = translationShortcut };
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Loads the independent UI-language preference and selected catalog-generation profile.</summary>
    public static LocalizationPreferences LoadLocalizationPreferences()
    {
        string path = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        return new LocalizationPreferences(root["preferEnglishUi"]?.GetValue<bool>() ?? false, root["localizationProfileId"]?.GetValue<string>() ?? "local-default");
    }

    /// <summary>Saves locale generation choices without changing translation direction preferences.</summary>
    public static void SaveLocalizationPreferences(bool preferEnglishUi, string profileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ConfigurationSnapshot configuration = LoadConfiguration();
        ModelProfile profile = configuration.Profiles.SingleOrDefault(candidate => candidate.Id.Equals(profileId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Choose an available profile for catalog generation.");
        string path = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        root["preferEnglishUi"] = preferEnglishUi;
        root["localizationProfileId"] = profileId;
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Loads the four persisted action-preset references in their displayed order.</summary>
    public static IReadOnlyList<string> LoadActionPresetIds()
    {
        string path = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        return root["actionPresets"]?.AsArray().Select(item => item?.GetValue<string>() ?? string.Empty).Take(4).ToArray()
            ?? ["correct", "rewrite", "summarize", "translate"];
    }

    /// <summary>Persists the action selected for one of the four quick presets.</summary>
    public static void SaveActionPreset(int slot, string actionId)
    {
        if (slot is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(slot));
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        string path = EnsureCreated();
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        JsonArray presets = root["actionPresets"]?.AsArray() ?? new JsonArray();
        while (presets.Count < 4) presets.Add(new[] { "correct", "rewrite", "summarize", "translate" }[presets.Count]);
        presets[slot - 1] = actionId;
        root["actionPresets"] = presets;
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void ValidateLocalSettings(TextTransformationSettings settings)
    {
        if (!Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out Uri? endpoint) || endpoint.Scheme is not "http" and not "https")
            throw new ArgumentException("Enter a valid HTTP Ollama endpoint.", nameof(settings));
        if (!IsLoopbackHost(endpoint.Host)) throw new ArgumentException("This device only allows localhost, 127.0.0.1, or ::1.", nameof(settings));
        if (settings.Temperature is < 0 or > 2) throw new ArgumentException("Temperature must be between 0 and 2.", nameof(settings));
        if (settings.Timeout <= TimeSpan.Zero) throw new ArgumentException("Timeout must be greater than zero.", nameof(settings));
        if (settings.ContextSize < 512) throw new ArgumentException("Context size must be at least 512 tokens.", nameof(settings));
    }

    private static void UpgradeVersionOneConfiguration(string path)
    {
        JsonObject root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new InvalidOperationException("The TextAid configuration is invalid.");
        JsonArray connections = root["connections"]?.AsArray() ?? throw new InvalidOperationException("The TextAid connections are missing.");
        JsonArray profiles = root["profiles"]?.AsArray() ?? throw new InvalidOperationException("The TextAid profiles are missing.");
        bool changed = false;
        changed |= AddConnectionIfMissing(connections, "ollama-network", "OnPremises", "ollama", "", false, "None", null);
        changed |= AddConnectionIfMissing(connections, "openai-external", "External", "openai-compatible", "", false, "ApiKey", "external-api-key");
        foreach (JsonObject connection in connections.OfType<JsonObject>())
        {
            string connectionId = connection["id"]?.GetValue<string>() ?? string.Empty;
            bool categoryWasMissing = connection["category"] is null;
            if (categoryWasMissing)
            {
                connection["category"] = connectionId.Equals("ollama-local", StringComparison.OrdinalIgnoreCase)
                    ? "ThisDeviceOnly"
                    : "OnPremises";
                changed = true;
            }

            bool isLocal = connection["category"]?.GetValue<string>() == "ThisDeviceOnly";
            if (connection["isEnabled"] is null || (categoryWasMissing && isLocal))
            {
                connection["isEnabled"] = isLocal;
                changed = true;
            }
        }
        changed |= AddProfileIfMissing(profiles, "network-default", "ollama-network");
        changed |= AddProfileIfMissing(profiles, "external-default", "openai-external");
        if (root["userLanguage"] is null) { root["userLanguage"] = "en"; changed = true; }
        if (root["preferredTranslationLanguage"] is null) { root["preferredTranslationLanguage"] = "fr"; changed = true; }
        if (root["shortcuts"] is null) { root["shortcuts"] = new JsonObject { ["normalAction"] = "Ctrl+C+C", ["quickTranslation"] = "Ctrl+C+T" }; changed = true; }
        if (root["actionPresets"] is null) { root["actionPresets"] = new JsonArray("correct", "rewrite", "summarize", "translate"); changed = true; }
        if (root["fullDebugMode"] is null) { root["fullDebugMode"] = false; changed = true; }
        if (root["preferEnglishUi"] is null) { root["preferEnglishUi"] = false; changed = true; }
        if (root["localizationProfileId"] is null) { root["localizationProfileId"] = "local-default"; changed = true; }
        if (changed) File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static bool AddConnectionIfMissing(JsonArray connections, string id, string category, string provider, string endpoint, bool isEnabled, string authentication, string? secretReference)
    {
        if (connections.Any(item => item?["id"]?.GetValue<string>()?.Equals(id, StringComparison.OrdinalIgnoreCase) == true)) return false;
        connections.Add(new JsonObject { ["id"] = id, ["category"] = category, ["provider"] = provider, ["endpoint"] = endpoint, ["isEnabled"] = isEnabled, ["authentication"] = authentication, ["secretReference"] = secretReference });
        return true;
    }

    private static bool AddProfileIfMissing(JsonArray profiles, string id, string connectionId)
    {
        if (profiles.Any(item => item?["id"]?.GetValue<string>()?.Equals(id, StringComparison.OrdinalIgnoreCase) == true)) return false;
        profiles.Add(new JsonObject { ["id"] = id, ["connectionId"] = connectionId, ["model"] = "", ["temperature"] = 0.2, ["timeoutSeconds"] = 120, ["providerOptions"] = new JsonObject() });
        return true;
    }

    /// <summary>Adds the remote definitions required by Settings when an earlier local-only configuration is saved.</summary>
    private static void EnsureRemoteConnectionDefinitions(JsonArray connections, JsonArray profiles)
    {
        AddConnectionIfMissing(connections, "ollama-network", "OnPremises", "ollama", "", false, "None", null);
        AddConnectionIfMissing(connections, "openai-external", "External", "openai-compatible", DefaultExternalEndpoint, false, "ApiKey", "external-api-key");
        AddProfileIfMissing(profiles, "network-default", "ollama-network");
        AddProfileIfMissing(profiles, "external-default", "openai-external");
    }

    private static void SetConnectionActivation(JsonArray connections, string id, bool isEnabled)
    {
        JsonObject connection = connections.FirstOrDefault(item => item?["id"]?.GetValue<string>()?.Equals(id, StringComparison.OrdinalIgnoreCase) == true)?.AsObject()
            ?? throw new InvalidOperationException($"The '{id}' connection is missing.");
        connection["isEnabled"] = isEnabled;
    }

    private static void SaveRemoteConnection(JsonArray connections, JsonArray profiles, string connectionId, string profileId, string endpoint, string model, string? secret, string secretReference)
    {
        JsonObject connection = connections.FirstOrDefault(item => item?["id"]?.GetValue<string>()?.Equals(connectionId, StringComparison.OrdinalIgnoreCase) == true)?.AsObject()
            ?? throw new InvalidOperationException($"The '{connectionId}' connection is missing.");
        JsonObject profile = profiles.FirstOrDefault(item => item?["id"]?.GetValue<string>()?.Equals(profileId, StringComparison.OrdinalIgnoreCase) == true)?.AsObject()
            ?? throw new InvalidOperationException($"The '{profileId}' profile is missing.");
        connection["endpoint"] = endpoint.Trim();
        profile["model"] = model.Trim();
        if (string.IsNullOrWhiteSpace(secret)) return;

        new DpapiSecretVault().SetSecret(secretReference, secret);
        connection["authentication"] = "ApiKey";
        connection["secretReference"] = secretReference;
    }

    private static void ValidateOptionalEndpoint(string endpoint, ConnectionCategory category)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? parsed) || parsed.Scheme is not "http" and not "https")
            throw new ArgumentException($"Enter a valid HTTP endpoint for {category}.", nameof(endpoint));
        if (category == ConnectionCategory.ThisDeviceOnly && !IsLoopbackHost(parsed.Host))
            throw new ArgumentException("This device only allows localhost, 127.0.0.1, or ::1.", nameof(endpoint));
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

    /// <summary>Returns the current user's TextAid application-data directory.</summary>
    public static string GetUserDataDirectory() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TextAid");

    /// <summary>Returns the current user's local TextAid data directory for machine-local diagnostics.</summary>
    public static string GetLocalUserDataDirectory() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TextAid");

    /// <summary>Determines whether a host is restricted to the current device.</summary>
    public static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        IPAddress.TryParse(host, out IPAddress? address) && IPAddress.IsLoopback(address);

    private static ThinkingMode ReadThinkingMode(JsonElement profile)
    {
        if (!profile.TryGetProperty("providerOptions", out JsonElement options) || !options.TryGetProperty("think", out JsonElement think)) return ThinkingMode.Off;
        return think.ValueKind == JsonValueKind.String && Enum.TryParse(think.GetString(), true, out ThinkingMode mode) ? mode : ThinkingMode.Off;
    }

    private static TEnum ReadEnum<TEnum>(JsonElement item, string name, TEnum fallback) where TEnum : struct, Enum =>
        item.TryGetProperty(name, out JsonElement value) && Enum.TryParse(value.GetString(), true, out TEnum parsed) ? parsed : fallback;

    private static void EnsureUnique(IEnumerable<string> ids, string type)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string id in ids)
        {
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id)) throw new InvalidOperationException($"A {type} ID is missing or duplicated.");
        }
    }

    private static void ValidateLanguage(string language, string name)
    {
        if (!LanguageCatalog.IsSupported(language))
            throw new InvalidOperationException($"The {name} is not supported.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(language, "^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$"))
            throw new InvalidOperationException($"The {name} is invalid.");
        try { _ = System.Globalization.CultureInfo.GetCultureInfo(language); }
        catch (System.Globalization.CultureNotFoundException) { throw new InvalidOperationException($"The {name} is invalid."); }
    }

    private static bool IsSupportedShortcut(string shortcut) =>
        !string.IsNullOrWhiteSpace(shortcut) && System.Text.RegularExpressions.Regex.IsMatch(shortcut, "^[A-Za-z0-9]+(\\+[A-Za-z0-9]+)+$");
}

/// <summary>Contains UI-language fallback and catalog-generation profile preferences.</summary>
public sealed record LocalizationPreferences(bool PreferEnglishUi, string ProfileId);
