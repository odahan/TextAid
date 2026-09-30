using System.Text.Json;

namespace TextAid.Core;

/// <summary>Generates a locale cache without allowing a provider or validation failure to replace English.</summary>
public sealed class LocaleCatalogGenerator(LocalizationCatalog catalog)
{
    private readonly LocalizationCatalog catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    /// <summary>Requests one catalog and persists it only after complete validation.</summary>
    public async Task<LocaleCatalogGenerationResult> GenerateAsync(
        string cachePath,
        Func<CancellationToken, Task<string>> requestCatalog,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cachePath);
        ArgumentNullException.ThrowIfNull(requestCatalog);

        try
        {
            string generated = await requestCatalog(cancellationToken);
            return catalog.TrySave(cachePath, generated)
                ? new(true, LocaleCatalogGenerationStatus.Created)
                : new(false, LocaleCatalogGenerationStatus.InvalidCatalog);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new(false, LocaleCatalogGenerationStatus.ProviderUnavailable);
        }
    }

    /// <summary>Generates a large locale catalog in validated batches and replaces the cache only when every batch succeeds.</summary>
    public async Task<LocaleCatalogGenerationResult> GenerateInBatchesAsync(
        string cachePath,
        int batchSize,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task<string>> requestBatch,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cachePath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        ArgumentNullException.ThrowIfNull(requestBatch);

        try
        {
            var translations = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string>[] entries in catalog.SourceValues
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Chunk(batchSize))
            {
                var source = entries.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
                string response = await requestBatch(source, cancellationToken);
                if (!new LocalizationCatalog(source).TryValidate(response, out IReadOnlyDictionary<string, string> translated))
                    return new(false, LocaleCatalogGenerationStatus.InvalidCatalog);

                foreach ((string key, string value) in translated) translations.Add(key, value);
            }

            return catalog.TrySave(cachePath, JsonSerializer.Serialize(translations))
                ? new(true, LocaleCatalogGenerationStatus.Created)
                : new(false, LocaleCatalogGenerationStatus.InvalidCatalog);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new(false, LocaleCatalogGenerationStatus.ProviderUnavailable);
        }
    }
}

/// <summary>Describes whether a generated locale cache was safely created.</summary>
public enum LocaleCatalogGenerationStatus { Created, InvalidCatalog, ProviderUnavailable }

/// <summary>Returns the safe outcome of a locale-cache generation request.</summary>
public sealed record LocaleCatalogGenerationResult(bool Succeeded, LocaleCatalogGenerationStatus Status);
