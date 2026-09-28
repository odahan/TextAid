using TextAid.Core;

namespace TextAid.Core.Tests;

public sealed class LocalizationCatalogTests
{
    private readonly LocalizationCatalog catalog = new(new Dictionary<string, string> { ["Greeting"] = "Hello {name}", ["Save"] = "Save" });

    [Fact]
    public void TryValidate_AcceptsCompleteCatalogWithPreservedPlaceholders() => Assert.True(catalog.TryValidate("{\"Greeting\":\"Bonjour {name}\",\"Save\":\"Enregistrer\"}", out _));

    [Fact]
    public void SourceFingerprint_IsStableForTheSameEnglishSource()
    {
        var equivalent = new LocalizationCatalog(new Dictionary<string, string> { ["Save"] = "Save", ["Greeting"] = "Hello {name}" });
        Assert.Equal(catalog.SourceFingerprint, equivalent.SourceFingerprint);
    }

    [Fact]
    public void LoadOrEnglish_RejectsACacheFromAnotherEnglishSource()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TextAid.Tests", Guid.NewGuid().ToString("N"));
        string cachePath = Path.Combine(directory, "fr.json");
        try
        {
            Assert.True(catalog.TrySave(cachePath, "{\"Greeting\":\"Bonjour {name}\",\"Save\":\"Enregistrer\"}"));
            var changedSource = new LocalizationCatalog(new Dictionary<string, string> { ["Greeting"] = "Hello {name}", ["Save"] = "Save changes" });
            LocaleCatalogLoadResult result = changedSource.Load(cachePath);
            Assert.Equal(LocaleCatalogLoadStatus.Stale, result.Status);
            Assert.Equal("Save changes", result.Values["Save"]);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("{\"Greeting\":\"Bonjour\",\"Save\":\"Enregistrer\"}")]
    [InlineData("{\"Greeting\":\"Bonjour {name}\",\"Extra\":\"x\"}")]
    [InlineData("not json")]
    public void TryValidate_RejectsInvalidOrIncompatibleCatalogs(string json) => Assert.False(catalog.TryValidate(json, out _));

    [Fact]
    public async Task GenerateAsync_ProviderFailureLeavesNoCacheAndReportsEnglishSafeFallback()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TextAid.Tests", Guid.NewGuid().ToString("N"));
        string cachePath = Path.Combine(directory, "fr.json");
        try
        {
            var generator = new LocaleCatalogGenerator(catalog);

            LocaleCatalogGenerationResult result = await generator.GenerateAsync(
                cachePath,
                _ => throw new HttpRequestException("Provider unavailable."),
                CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal(LocaleCatalogGenerationStatus.ProviderUnavailable, result.Status);
            Assert.False(File.Exists(cachePath));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task GenerateAsync_InvalidCatalogDoesNotReplaceTheExistingValidatedCache()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TextAid.Tests", Guid.NewGuid().ToString("N"));
        string cachePath = Path.Combine(directory, "fr.json");
        try
        {
            Assert.True(catalog.TrySave(cachePath, "{\"Greeting\":\"Bonjour {name}\",\"Save\":\"Enregistrer\"}"));
            var generator = new LocaleCatalogGenerator(catalog);

            LocaleCatalogGenerationResult result = await generator.GenerateAsync(cachePath, _ => Task.FromResult("{\"Greeting\":\"Bonjour\"}"), CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal(LocaleCatalogGenerationStatus.InvalidCatalog, result.Status);
            Assert.Equal(LocaleCatalogLoadStatus.Loaded, catalog.Load(cachePath).Status);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void Load_LockedCacheReturnsEnglishWithUnreadableStatus()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TextAid.Tests", Guid.NewGuid().ToString("N"));
        string cachePath = Path.Combine(directory, "fr.json");
        try
        {
            Assert.True(catalog.TrySave(cachePath, "{\"Greeting\":\"Bonjour {name}\",\"Save\":\"Enregistrer\"}"));
            using (var lockStream = new FileStream(cachePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                LocaleCatalogLoadResult result = catalog.Load(cachePath);

                Assert.Equal(LocaleCatalogLoadStatus.Unreadable, result.Status);
                Assert.Equal("Hello {name}", result.Values["Greeting"]);
            }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void TryDelete_RemovesOnlyTheGeneratedCacheAndItsMetadata()
    {
        string directory = CreateDirectory();
        try
        {
            string cachePath = Path.Combine(directory, "fr.json");
            string overridesPath = LanguagePackService.GetOverridesPath(directory, "fr");
            Assert.True(catalog.TrySave(cachePath, "{\"Greeting\":\"Bonjour {name}\",\"Save\":\"Enregistrer\"}"));
            File.WriteAllText(cachePath + ".origin.json", "{}");
            Assert.True(new LanguagePackService(catalog).TrySaveOverride(overridesPath, "Save", "Sauvegarder"));

            Assert.True(catalog.TryDelete(cachePath));

            Assert.False(File.Exists(cachePath));
            Assert.False(File.Exists(cachePath + ".source-fingerprint"));
            Assert.False(File.Exists(cachePath + ".origin.json"));
            Assert.True(File.Exists(overridesPath));
        }
        finally { DeleteDirectory(directory); }
    }

    [Fact]
    public void PersonalOverride_TakesPrecedenceAndCanBeRestored()
    {
        string directory = CreateDirectory();
        try
        {
            var service = new LanguagePackService(catalog);
            string overridesPath = LanguagePackService.GetOverridesPath(directory, "fr");
            Assert.True(service.TrySaveOverride(overridesPath, "Save", "Sauvegarder"));
            Assert.Equal("Sauvegarder", service.ApplyOverrides(new Dictionary<string, string> { ["Greeting"] = "Bonjour {name}", ["Save"] = "Enregistrer" }, overridesPath)["Save"]);
            Assert.True(service.TryRestoreSuggested(overridesPath, "Save"));
            Assert.Equal("Enregistrer", service.ApplyOverrides(new Dictionary<string, string> { ["Greeting"] = "Bonjour {name}", ["Save"] = "Enregistrer" }, overridesPath)["Save"]);
        }
        finally { DeleteDirectory(directory); }
    }

    [Fact]
    public void Import_ValidPackReplacesSuggestedCacheButPreservesPersonalOverrides()
    {
        string directory = CreateDirectory();
        try
        {
            string locales = Path.Combine(directory, "locales");
            string cache = Path.Combine(locales, "fr.json");
            Assert.True(catalog.TrySave(cache, "{\"Greeting\":\"Bonjour {name}\",\"Save\":\"Enregistrer\"}"));
            var service = new LanguagePackService(catalog);
            string overridesPath = LanguagePackService.GetOverridesPath(locales, "fr");
            Assert.True(service.TrySaveOverride(overridesPath, "Save", "Sauvegarder"));
            string pack = Path.Combine(directory, "reviewed.json");
            Assert.True(service.TryExport(pack, "fr", cache, "community/example", new Dictionary<string, string> { ["reviewedBy"] = "Community" }, out _));

            LanguagePackImportResult result = service.Import(pack, locales);

            Assert.True(result.Succeeded);
            Assert.Equal(LocaleCatalogLoadStatus.Loaded, catalog.Load(cache).Status);
            Assert.Equal("Sauvegarder", service.ApplyOverrides(catalog.Load(cache).Values, overridesPath)["Save"]);
            Assert.Equal("Imported pack", service.GetOrigin(cache, overridesPath, "Greeting").Kind);
            Assert.Equal("Personal override", service.GetOrigin(cache, overridesPath, "Save").Kind);
        }
        finally { DeleteDirectory(directory); }
    }

    [Theory]
    [InlineData("{\"formatVersion\":1,\"language\":\"fr\",\"sourceFingerprint\":\"wrong\",\"translations\":{\"Greeting\":\"Bonjour {name}\",\"Save\":\"Enregistrer\"},\"provenance\":\"test\",\"reviewMetadata\":{}}", LanguagePackImportStatus.Incompatible)]
    [InlineData("{\"formatVersion\":1,\"language\":\"fr\",\"sourceFingerprint\":\"FINGERPRINT\",\"translations\":{\"Greeting\":\"Bonjour\",\"Save\":\"Enregistrer\"},\"provenance\":\"test\",\"reviewMetadata\":{}}", LanguagePackImportStatus.InvalidTranslations)]
    [InlineData("not json", LanguagePackImportStatus.InvalidJson)]
    public void Import_RejectionPreservesExistingCache(string packContents, LanguagePackImportStatus expected)
    {
        string directory = CreateDirectory();
        try
        {
            string locales = Path.Combine(directory, "locales");
            string cache = Path.Combine(locales, "fr.json");
            const string existing = "{\"Greeting\":\"Bonjour {name}\",\"Save\":\"Enregistrer\"}";
            Assert.True(catalog.TrySave(cache, existing));
            string pack = Path.Combine(directory, "invalid.json");
            File.WriteAllText(pack, packContents.Replace("FINGERPRINT", catalog.SourceFingerprint, StringComparison.Ordinal));

            LanguagePackImportResult result = new LanguagePackService(catalog).Import(pack, locales);

            Assert.False(result.Succeeded);
            Assert.Equal(expected, result.Status);
            Assert.Equal(existing, File.ReadAllText(cache));
        }
        finally { DeleteDirectory(directory); }
    }

    private static string CreateDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TextAid.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
