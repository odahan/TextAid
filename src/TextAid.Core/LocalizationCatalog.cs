using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace TextAid.Core;

/// <summary>Validates and stores a generated locale catalog while preserving English as a safe fallback.</summary>
public sealed class LocalizationCatalog
{
    private readonly IReadOnlyDictionary<string, string> english;

    /// <summary>Creates a catalog validator from the authoritative English source strings.</summary>
    public LocalizationCatalog(IReadOnlyDictionary<string, string> english)
    {
        this.english = english ?? throw new ArgumentNullException(nameof(english));
    }

    /// <summary>Gets a stable SHA-256 identity for compatibility with future language packs.</summary>
    public string SourceFingerprint => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", english.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}")))));

    /// <summary>Validates a generated JSON object against source keys and interpolation placeholders.</summary>
    public bool TryValidate(string json, out IReadOnlyDictionary<string, string> catalog)
    {
        catalog = english;
        try
        {
            Dictionary<string, string>? proposed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (proposed is null || proposed.Count != english.Count || !proposed.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(english.Keys)) return false;
            foreach ((string key, string source) in english)
            {
                if (string.IsNullOrWhiteSpace(proposed[key]) || !Placeholders(source).SetEquals(Placeholders(proposed[key]))) return false;
            }
            catalog = proposed;
            return true;
        }
        catch (JsonException) { return false; }
    }

    /// <summary>Loads a valid cached catalog and reports why English was used when it could not be activated.</summary>
    public LocaleCatalogLoadResult Load(string cachePath)
    {
        try
        {
            string fingerprintPath = cachePath + ".source-fingerprint";
            if (!File.Exists(cachePath)) return new(english, LocaleCatalogLoadStatus.Missing);
            if (!File.Exists(fingerprintPath) || !File.ReadAllText(fingerprintPath).Trim().Equals(SourceFingerprint, StringComparison.OrdinalIgnoreCase)) return new(english, LocaleCatalogLoadStatus.Stale);
            return TryValidate(File.ReadAllText(cachePath), out IReadOnlyDictionary<string, string> catalog)
                ? new(catalog, LocaleCatalogLoadStatus.Loaded)
                : new(english, LocaleCatalogLoadStatus.Invalid);
        }
        catch (IOException) { return new(english, LocaleCatalogLoadStatus.Unreadable); }
    }

    /// <summary>Loads a valid cache or returns English for compatibility with simple callers.</summary>
    public IReadOnlyDictionary<string, string> LoadOrEnglish(string cachePath) => Load(cachePath).Values;

    /// <summary>Writes a validated catalog using replacement so an invalid partial file is never active.</summary>
    public bool TrySave(string cachePath, string json)
    {
        if (!TryValidate(json, out _)) return false;
        string temporary = cachePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath) ?? throw new InvalidOperationException("A cache directory is required."));
            File.WriteAllText(temporary, json);
            File.Move(temporary, cachePath, true);
            File.WriteAllText(cachePath + ".source-fingerprint", SourceFingerprint);
            return true;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static HashSet<string> Placeholders(string value) => System.Text.RegularExpressions.Regex.Matches(value, "\\{[^{}]+\\}").Select(match => match.Value).ToHashSet(StringComparer.Ordinal);
}

/// <summary>Identifies how a locale cache was resolved.</summary>
public enum LocaleCatalogLoadStatus { Loaded, Missing, Stale, Invalid, Unreadable }

/// <summary>Returns the activated locale values and their resolution status.</summary>
public sealed record LocaleCatalogLoadResult(IReadOnlyDictionary<string, string> Values, LocaleCatalogLoadStatus Status);
