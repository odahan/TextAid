using System.Globalization;
using System.Text.Json;

namespace TextAid.Core;

/// <summary>Stores personal translation corrections and validates portable language packs without changing a usable cache on failure.</summary>
public sealed class LanguagePackService
{
    private const int CurrentFormatVersion = 1;
    private readonly LocalizationCatalog catalog;

    /// <summary>Creates the service for one authoritative English catalog.</summary>
    public LanguagePackService(LocalizationCatalog catalog) => this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    /// <summary>Returns the path of the isolated, per-language personal-correction file.</summary>
    public static string GetOverridesPath(string localesDirectory, string language) => Path.Combine(localesDirectory, "overrides", language + ".json");

    /// <summary>Combines a validated suggested cache with valid personal corrections.</summary>
    public IReadOnlyDictionary<string, string> ApplyOverrides(IReadOnlyDictionary<string, string> suggested, string overridesPath)
    {
        ArgumentNullException.ThrowIfNull(suggested);
        var values = new Dictionary<string, string>(suggested, StringComparer.Ordinal);
        foreach ((string key, string value) in LoadOverrides(overridesPath)) values[key] = value;
        return values;
    }

    /// <summary>Loads valid personal corrections. A damaged correction file is ignored so the suggested cache remains usable.</summary>
    public IReadOnlyDictionary<string, string> LoadOverrides(string overridesPath)
    {
        try
        {
            if (!File.Exists(overridesPath)) return new Dictionary<string, string>();
            Dictionary<string, string>? values = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(overridesPath));
            return values is not null && AreValidOverrides(values) ? values : new Dictionary<string, string>();
        }
        catch (IOException) { return new Dictionary<string, string>(); }
        catch (JsonException) { return new Dictionary<string, string>(); }
    }

    /// <summary>Saves one personal correction after ensuring that its key and placeholders remain compatible with English.</summary>
    public bool TrySaveOverride(string overridesPath, string key, string value)
    {
        Dictionary<string, string> overrides = new(LoadOverrides(overridesPath), StringComparer.Ordinal) { [key] = value };
        if (!AreValidOverrides(overrides)) return false;
        return TryWriteAtomically(overridesPath, JsonSerializer.Serialize(overrides, JsonOptions));
    }

    /// <summary>Removes one personal correction, restoring the active suggested translation for that key.</summary>
    public bool TryRestoreSuggested(string overridesPath, string key)
    {
        Dictionary<string, string> overrides = new(LoadOverrides(overridesPath), StringComparer.Ordinal);
        if (!overrides.Remove(key)) return true;
        return TryWriteAtomically(overridesPath, JsonSerializer.Serialize(overrides, JsonOptions));
    }

    /// <summary>Exports a complete validated suggested cache. Personal corrections are deliberately excluded.</summary>
    public bool TryExport(string packPath, string language, string cachePath, string provenance, IReadOnlyDictionary<string, string>? reviewMetadata, out string error)
    {
        error = string.Empty;
        LocaleCatalogLoadResult cache = catalog.Load(cachePath);
        if (cache.Status != LocaleCatalogLoadStatus.Loaded)
        {
            error = "A compatible suggested translation cache is required before exporting a language pack.";
            return false;
        }

        var pack = new LanguagePack(CurrentFormatVersion, language, catalog.SourceFingerprint, new Dictionary<string, string>(cache.Values), provenance, reviewMetadata ?? new Dictionary<string, string>());
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(packPath) ?? throw new InvalidOperationException("A pack directory is required."));
            File.WriteAllText(packPath, JsonSerializer.Serialize(pack, JsonOptions));
            return true;
        }
        catch (IOException)
        {
            error = "TextAid could not write the language pack.";
            return false;
        }
    }

    /// <summary>Validates an external pack completely before replacing its language cache, preserving corrections and the old cache on every failure.</summary>
    public LanguagePackImportResult Import(string packPath, string localesDirectory)
    {
        try
        {
            LanguagePack? pack = JsonSerializer.Deserialize<LanguagePack>(File.ReadAllText(packPath), JsonOptions);
            if (pack is null || pack.Translations is null || pack.ReviewMetadata is null || pack.FormatVersion != CurrentFormatVersion || !IsBcp47(pack.Language) || !pack.SourceFingerprint.Equals(catalog.SourceFingerprint, StringComparison.OrdinalIgnoreCase)) return new(false, null, LanguagePackImportStatus.Incompatible);
            string json = JsonSerializer.Serialize(pack.Translations, JsonOptions);
            if (!catalog.TryValidate(json, out _)) return new(false, null, LanguagePackImportStatus.InvalidTranslations);

            string cachePath = Path.Combine(localesDirectory, pack.Language + ".json");
            if (!TryWriteAtomically(cachePath + ".origin.json", JsonSerializer.Serialize(new TranslationOrigin("Imported pack", pack.Provenance, pack.ReviewMetadata), JsonOptions))) return new(false, null, LanguagePackImportStatus.StorageFailure);
            if (!catalog.TrySave(cachePath, json)) return new(false, null, LanguagePackImportStatus.StorageFailure);
            return new(true, pack.Language, LanguagePackImportStatus.Imported);
        }
        catch (IOException) { return new(false, null, LanguagePackImportStatus.Unreadable); }
        catch (JsonException) { return new(false, null, LanguagePackImportStatus.InvalidJson); }
        catch (ArgumentException) { return new(false, null, LanguagePackImportStatus.InvalidJson); }
    }

    /// <summary>Reports whether the active text is generated locally, imported, or overridden personally.</summary>
    public TranslationOrigin GetOrigin(string cachePath, string overridesPath, string key)
    {
        if (LoadOverrides(overridesPath).ContainsKey(key)) return new("Personal override", null, new Dictionary<string, string>());
        try
        {
            if (File.Exists(cachePath + ".origin.json"))
            {
                TranslationOrigin? origin = JsonSerializer.Deserialize<TranslationOrigin>(File.ReadAllText(cachePath + ".origin.json"), JsonOptions);
                if (origin is not null) return origin;
            }
        }
        catch (IOException) { }
        catch (JsonException) { }
        return new("Generated locally", null, new Dictionary<string, string>());
    }

    private bool AreValidOverrides(IReadOnlyDictionary<string, string> values)
    {
        foreach ((string key, string value) in values)
        {
            if (!catalog.SourceValues.TryGetValue(key, out string? source) || string.IsNullOrWhiteSpace(value) || !LocalizationCatalog.Placeholders(source).SetEquals(LocalizationCatalog.Placeholders(value))) return false;
        }
        return true;
    }

    private static bool IsBcp47(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$")) return false;
        try { _ = CultureInfo.GetCultureInfo(value); return true; }
        catch (CultureNotFoundException) { return false; }
    }

    private static bool TryWriteAtomically(string path, string text)
    {
        string temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("A destination directory is required."));
            File.WriteAllText(temporary, text);
            File.Move(temporary, path, true);
            return true;
        }
        catch (IOException) { return false; }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
}

/// <summary>Describes the portable JSON representation of a reviewed language pack.</summary>
public sealed record LanguagePack(int FormatVersion, string Language, string SourceFingerprint, Dictionary<string, string> Translations, string Provenance, IReadOnlyDictionary<string, string> ReviewMetadata);

/// <summary>Describes where an active translation value came from.</summary>
public sealed record TranslationOrigin(string Kind, string? Provenance, IReadOnlyDictionary<string, string> ReviewMetadata);

/// <summary>Describes a language-pack import result without exposing partial activation.</summary>
public sealed record LanguagePackImportResult(bool Succeeded, string? Language, LanguagePackImportStatus Status);

/// <summary>Identifies why a pack was accepted or rejected.</summary>
public enum LanguagePackImportStatus { Imported, InvalidJson, Incompatible, InvalidTranslations, Unreadable, StorageFailure }
