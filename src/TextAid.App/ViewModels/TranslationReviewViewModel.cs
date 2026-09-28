using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextAid.App.Localization;
using TextAid.Core;

namespace TextAid.App.ViewModels;

/// <summary>Edits the local correction layer for the currently selected UI language.</summary>
public sealed partial class TranslationReviewViewModel : ObservableObject
{
    private readonly LocalizationCatalog catalog;
    private readonly LanguagePackService packs;
    private readonly string localesDirectory;
    private readonly string language;

    [ObservableProperty] private TranslationReviewItem? selectedItem;
    [ObservableProperty] private string status = string.Empty;

    /// <summary>Creates a review session that never changes the English source or suggested cache directly.</summary>
    public TranslationReviewViewModel(string language)
    {
        this.language = language;
        catalog = new LocalizationCatalog(EnglishStringCatalog.Values);
        packs = new LanguagePackService(catalog);
        localesDirectory = Path.Combine(UserConfiguration.GetUserDataDirectory(), "locales");
        Reload();
    }

    /// <summary>Gets the language being reviewed.</summary>
    public string Language => language;

    /// <summary>Gets the rows available for review.</summary>
    public ObservableCollection<TranslationReviewItem> Items { get; } = [];

    /// <summary>Raised after an accepted correction or compatible import changes displayed translations.</summary>
    public event EventHandler? TranslationsChanged;

    /// <summary>Requests confirmation before the generated suggested-translation cache is removed.</summary>
    public event Func<bool>? DeleteCacheConfirmationRequested;

    /// <summary>Raised after deletion returns the application to its English default UI state.</summary>
    public event EventHandler? LanguageCacheDeleted;

    [RelayCommand]
    private void SaveCorrection()
    {
        if (SelectedItem is null) return;
        if (packs.TrySaveOverride(OverridesPath, SelectedItem.Key, SelectedItem.PersonalOverride ?? string.Empty))
        {
            SelectedItem.Origin = packs.GetOrigin(CachePath, OverridesPath, SelectedItem.Key).Kind;
            Status = UiStrings.Get("TranslationCorrectionSavedMessage");
            TranslationsChanged?.Invoke(this, EventArgs.Empty);
            return;
        }
        Status = UiStrings.Get("TranslationCorrectionInvalidMessage");
    }

    [RelayCommand]
    private void RestoreSuggested()
    {
        if (SelectedItem is null) return;
        if (!packs.TryRestoreSuggested(OverridesPath, SelectedItem.Key))
        {
            Status = UiStrings.Get("TranslationReviewStorageFailureMessage");
            return;
        }
        SelectedItem.PersonalOverride = string.Empty;
        SelectedItem.Origin = packs.GetOrigin(CachePath, OverridesPath, SelectedItem.Key).Kind;
        Status = UiStrings.Get("TranslationRestoredMessage");
        TranslationsChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void DeleteLanguageCache()
    {
        if (DeleteCacheConfirmationRequested?.Invoke() != true) return;
        if (!catalog.TryDelete(CachePath))
        {
            Status = UiStrings.Get("LanguageCacheDeletionFailureMessage");
            return;
        }

        try
        {
            ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
            LocalizationPreferences localization = UserConfiguration.LoadLocalizationPreferences();
            UserConfiguration.SaveUserPreferences("en", configuration.PreferredTranslationLanguage, configuration.NormalShortcut, configuration.TranslationShortcut);
            UserConfiguration.SaveLocalizationPreferences(true, localization.ProfileId);
            Status = UiStrings.Get("LanguageCacheDeletedMessage");
            TranslationsChanged?.Invoke(this, EventArgs.Empty);
            LanguageCacheDeleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            Status = UiStrings.Get("LanguageCacheDeletionFailureMessage");
        }
    }

    /// <summary>Exports the compatible suggested cache, intentionally excluding personal corrections.</summary>
    public bool Export(string path)
    {
        bool succeeded = packs.TryExport(path, language, CachePath, "TextAid local export", new Dictionary<string, string> { ["reviewStatus"] = "User exported" }, out _);
        Status = UiStrings.Get(succeeded ? "LanguagePackExportedMessage" : "LanguagePackExportFailureMessage");
        return succeeded;
    }

    /// <summary>Imports a fully compatible selected pack before updating review rows.</summary>
    public bool Import(string path)
    {
        LanguagePackImportResult result = packs.Import(path, localesDirectory);
        if (!result.Succeeded)
        {
            Status = UiStrings.Get("LanguagePackImportFailureMessage");
            return false;
        }
        if (result.Language!.Equals(language, StringComparison.OrdinalIgnoreCase))
        {
            Reload();
            TranslationsChanged?.Invoke(this, EventArgs.Empty);
        }
        Status = UiStrings.Get("LanguagePackImportedMessage");
        return true;
    }

    /// <summary>Persists every edited correction before the editor closes, keeping the window open if a value is invalid.</summary>
    public bool TryPersistPendingCorrections()
    {
        foreach (TranslationReviewItem item in Items)
        {
            bool saved = string.IsNullOrWhiteSpace(item.PersonalOverride)
                ? packs.TryRestoreSuggested(OverridesPath, item.Key)
                : packs.TrySaveOverride(OverridesPath, item.Key, item.PersonalOverride);
            if (!saved)
            {
                SelectedItem = item;
                Status = UiStrings.Get("TranslationCorrectionInvalidMessage");
                return false;
            }
            item.Origin = packs.GetOrigin(CachePath, OverridesPath, item.Key).Kind;
        }

        TranslationsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private string CachePath => Path.Combine(localesDirectory, language + ".json");
    private string OverridesPath => LanguagePackService.GetOverridesPath(localesDirectory, language);

    private void Reload()
    {
        LocaleCatalogLoadResult loaded = catalog.Load(CachePath);
        IReadOnlyDictionary<string, string> overrides = packs.LoadOverrides(OverridesPath);
        Items.Clear();
        foreach ((string key, string english) in catalog.SourceValues.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            string suggested = loaded.Status == LocaleCatalogLoadStatus.Loaded ? loaded.Values[key] : english;
            overrides.TryGetValue(key, out string? personalOverride);
            Items.Add(new TranslationReviewItem(key, english, suggested, personalOverride ?? string.Empty, packs.GetOrigin(CachePath, OverridesPath, key).Kind));
        }
        SelectedItem = Items.FirstOrDefault();
        Status = loaded.Status == LocaleCatalogLoadStatus.Loaded ? string.Empty : UiStrings.Get("TranslationReviewEnglishFallbackMessage");
    }
}

/// <summary>Represents a stable catalog entry and its current correction state.</summary>
public sealed partial class TranslationReviewItem(string key, string english, string suggested, string personalOverride, string origin) : ObservableObject
{
    /// <summary>Gets the stable catalog identifier.</summary>
    public string Key { get; } = key;
    /// <summary>Gets the authoritative English text.</summary>
    public string English { get; } = english;
    /// <summary>Gets the active suggested translation.</summary>
    public string Suggested { get; } = suggested;
    [ObservableProperty] private string personalOverride = personalOverride;
    [ObservableProperty] private string origin = origin;
}
