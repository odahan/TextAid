using System.Text.Json;

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
                profiles = new[] { new { id = "local-default", connectionId = "ollama-local", model = "", temperature = 0.2, timeoutSeconds = 120, providerOptions = new { think = false } } }
            };
            File.WriteAllText(path, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }
        return path;
    }
}
