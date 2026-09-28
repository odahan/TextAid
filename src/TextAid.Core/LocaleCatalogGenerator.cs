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
}

/// <summary>Describes whether a generated locale cache was safely created.</summary>
public enum LocaleCatalogGenerationStatus { Created, InvalidCatalog, ProviderUnavailable }

/// <summary>Returns the safe outcome of a locale-cache generation request.</summary>
public sealed record LocaleCatalogGenerationResult(bool Succeeded, LocaleCatalogGenerationStatus Status);
