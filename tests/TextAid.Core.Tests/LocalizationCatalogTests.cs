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
}
