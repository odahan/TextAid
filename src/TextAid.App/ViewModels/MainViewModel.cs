using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using TextAid.Core;
using TextAid.App.Localization;

namespace TextAid.App.ViewModels;

/// <summary>Exposes one short-lived result transaction to the session window.</summary>
public sealed class MainViewModel : ObservableObject
{
    private string inputText;
    private ActionDefinition? selectedAction;
    private readonly Action processPreset;
    private bool isFullLogActive;
    private bool isUiTranslationRequired;
    private ConnectionCategory? activeConnectionCategory;
    private IReadOnlyList<LanguageOption> outputLanguages = CreateOutputLanguages();
    private string detectedLanguageName = string.Empty;

    public MainViewModel(InvocationSession session, IReadOnlyList<ActionDefinition> actions, IReadOnlyList<string> presetActionIds, Action replace, Action copy, Action process, Action instructions, Action reset, Action free, Action close, string status, bool isFullLogActive, bool isUiTranslationRequired)
    {
        Session = session;
        Session.LanguageDetectionChanged += (_, _) => UpdateDetectedLanguage();
        inputText = session.InputText;
        UpdateDetectedLanguage();
        Actions = new ObservableCollection<ActionDefinition>(actions);
        Presets = new ObservableCollection<ActionPreset>(Enumerable.Range(1, 4).Select(slot => new ActionPreset(slot, FindAction(presetActionIds.ElementAtOrDefault(slot - 1)))));
        selectedAction = Actions.FirstOrDefault(action => action.Id.Equals(session.ActionId, StringComparison.OrdinalIgnoreCase)) ?? Actions.FirstOrDefault();
        Session.ActionId = selectedAction?.Id;
        if (Session.OutputLanguage == "Unchanged") Session.OutputLanguage = selectedAction?.OutputLanguageDefault ?? "Unchanged";
        Status = status;
        this.isFullLogActive = isFullLogActive;
        this.isUiTranslationRequired = isUiTranslationRequired;
        processPreset = process;
        ReplaceCommand = new RelayCommand(replace, () => CanReplaceResult);
        CopyCommand = new RelayCommand(copy, () => CanUseResult);
        ProcessCommand = new RelayCommand(process, () => CanProcess);
        InstructionsCommand = new RelayCommand(instructions, () => !IsTransforming);
        NewCommand = new RelayCommand(reset, () => !IsTransforming);
        FreeCommand = new RelayCommand(free, () => !IsTransforming);
        CancelCommand = new RelayCommand(close);
        SelectPresetCommand = new RelayCommand<ActionPreset>(SelectPreset, preset => preset?.Action is not null && !IsTransforming);
    }

    public InvocationSession Session { get; }
    public ObservableCollection<ActionDefinition> Actions { get; }
    public IReadOnlyList<LanguageOption> OutputLanguages => outputLanguages;
    public string SelectedOutputLanguage
    {
        get => Session.OutputLanguage;
        set
        {
            string selectedLanguage = string.IsNullOrWhiteSpace(value) ? "Unchanged" : value;
            if (string.Equals(Session.OutputLanguage, selectedLanguage, StringComparison.OrdinalIgnoreCase)) return;
            Session.OutputLanguage = selectedLanguage;
            OnPropertyChanged();
            if (!string.IsNullOrWhiteSpace(InputText) && SelectedAction is not null) processPreset();
        }
    }
    public bool MarkdownOutputEnabled
    {
        get => Session.MarkdownOutputEnabled;
        set
        {
            if (Session.MarkdownOutputEnabled == value) return;
            Session.MarkdownOutputEnabled = value;
            OnPropertyChanged();
            if (!string.IsNullOrWhiteSpace(InputText) && SelectedAction is not null) processPreset();
        }
    }
    public ObservableCollection<ActionPreset> Presets { get; }
    public ActionDefinition? SelectedAction
    {
        get => selectedAction;
        set
        {
            if (!SetProperty(ref selectedAction, value)) return;
            Session.InvalidateResult();
            Session.ActionId = value?.Id;
            if (Session.OutputLanguage == "Unchanged")
            {
                Session.OutputLanguage = value?.OutputLanguageDefault ?? "Unchanged";
                OnPropertyChanged(nameof(SelectedOutputLanguage));
            }
            OnPropertyChanged(nameof(CanProcess));
            NotifyRequestChanged();
        }
    }
    public string InputText
    {
        get => inputText;
        set
        {
            if (!SetProperty(ref inputText, value)) return;
            Session.SetInputText(value);
            OnPropertyChanged(nameof(SelectedOutputLanguage));
            UpdateDetectedLanguage();
            NotifyRequestChanged();
        }
    }

    private void NotifyRequestChanged()
    {
        OnPropertyChanged(nameof(OutputText));
        OnPropertyChanged(nameof(IsTransforming));
        OnPropertyChanged(nameof(CanUseResult));
        OnPropertyChanged(nameof(CanReplaceResult));
        OnPropertyChanged(nameof(CanProcess));
        ReplaceCommand.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
        ProcessCommand.NotifyCanExecuteChanged();
        InstructionsCommand.NotifyCanExecuteChanged();
        NewCommand.NotifyCanExecuteChanged();
        FreeCommand.NotifyCanExecuteChanged();
        SelectPresetCommand.NotifyCanExecuteChanged();
    }
    public string? OutputText => Session.OutputText;
    /// <summary>Gets the AI-detected language name or the current detection status.</summary>
    public string DetectedLanguageName
    {
        get => detectedLanguageName;
        private set => SetProperty(ref detectedLanguageName, value);
    }
    public string Status { get; private set; }
    public bool IsFullLogActive
    {
        get => isFullLogActive;
        private set => SetProperty(ref isFullLogActive, value);
    }
    public bool IsUiTranslationRequired
    {
        get => isUiTranslationRequired;
        private set => SetProperty(ref isUiTranslationRequired, value);
    }
    /// <summary>Gets whether an invocation has resolved a connection category for the visible provider indicator.</summary>
    public bool HasActiveConnection => activeConnectionCategory is not null;
    /// <summary>Gets the deployment category actually selected for the current invocation.</summary>
    public ConnectionCategory? ActiveConnectionCategory => activeConnectionCategory;
    /// <summary>Gets a compact recognizable symbol for the selected deployment category.</summary>
    public string ActiveConnectionSymbol => activeConnectionCategory switch
    {
        ConnectionCategory.ThisDeviceOnly => "⌂",
        ConnectionCategory.OnPremises => "⌁",
        ConnectionCategory.External => "◎",
        _ => string.Empty
    };
    /// <summary>Gets accessible localized text describing the selected deployment category.</summary>
    public string ActiveConnectionLabel => activeConnectionCategory switch
    {
        ConnectionCategory.ThisDeviceOnly => UiStrings.Get("ThisDeviceOnlyIndicatorLabel"),
        ConnectionCategory.OnPremises => UiStrings.Get("OnPremisesIndicatorLabel"),
        ConnectionCategory.External => UiStrings.Get("ExternalIndicatorLabel"),
        _ => string.Empty
    };
    public string ActiveConnectionActivityLabel => activeConnectionCategory switch
    {
        ConnectionCategory.ThisDeviceOnly => UiStrings.Get("LocalModelWorkingLabel"),
        ConnectionCategory.OnPremises => UiStrings.Get("NetworkModelWorkingLabel"),
        ConnectionCategory.External => UiStrings.Get("ExternalModelWorkingLabel"),
        _ => UiStrings.Get("TransformingLabel")
    };
    public bool IsTransforming => Session.State == InvocationState.Transforming;
    public bool CanUseResult => Session.HasCurrentResult;
    public bool CanReplaceResult => CanUseResult && Session.SourceWindow != 0;
    public bool CanProcess => !IsTransforming && !string.IsNullOrWhiteSpace(InputText) && SelectedAction is not null;
    public IRelayCommand ReplaceCommand { get; }
    public IRelayCommand CopyCommand { get; }
    public IRelayCommand ProcessCommand { get; }
    public IRelayCommand InstructionsCommand { get; }
    public IRelayCommand NewCommand { get; }
    public IRelayCommand FreeCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand<ActionPreset> SelectPresetCommand { get; }

    /// <summary>Selects the action assigned to a quick preset.</summary>
    public void SelectPreset(ActionPreset? preset)
    {
        if (preset?.Action is null || IsTransforming) return;
        SelectedAction = preset.Action;
        processPreset();
    }

    /// <summary>Assigns an existing action to a quick preset and refreshes its caption.</summary>
    public void AssignPreset(int slot, ActionDefinition action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ActionPreset? preset = Presets.FirstOrDefault(candidate => candidate.Slot == slot);
        if (preset is null) return;
        preset.Action = action;
        OnPropertyChanged(nameof(Presets));
        SelectPresetCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Replaces the selectable action set after a validated Actions-page save.</summary>
    public void ReloadActions(IReadOnlyList<ActionDefinition> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        string? previousId = SelectedAction?.Id;
        Actions.Clear();
        foreach (ActionDefinition action in actions) Actions.Add(action);
        SelectedAction = Actions.FirstOrDefault(action => action.Id.Equals(previousId, StringComparison.OrdinalIgnoreCase)) ?? Actions.FirstOrDefault();
        foreach (ActionPreset preset in Presets) preset.Action = FindAction(preset.Action?.Id);
        OnPropertyChanged(nameof(Actions));
        SelectPresetCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Notifies action captions after the active UI locale changes.</summary>
    public void RefreshLocalizedActionLabels()
    {
        outputLanguages = CreateOutputLanguages();
        OnPropertyChanged(nameof(OutputLanguages));
        OnPropertyChanged(nameof(SelectedOutputLanguage));
        foreach (ActionPreset preset in Presets) preset.RefreshLabel();
        OnPropertyChanged(nameof(Actions));
    }

    /// <summary>Disables result actions and shows a safe-failure message.</summary>
    public void ShowFailure(string message)
    {
        Session.State = InvocationState.Failed;
        Status = message;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsTransforming));
        OnPropertyChanged(nameof(CanUseResult));
        OnPropertyChanged(nameof(CanReplaceResult));
        OnPropertyChanged(nameof(CanProcess));
        ReplaceCommand.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
        ProcessCommand.NotifyCanExecuteChanged();
        InstructionsCommand.NotifyCanExecuteChanged();
        NewCommand.NotifyCanExecuteChanged();
        FreeCommand.NotifyCanExecuteChanged();
        SelectPresetCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Publishes the completed local transformation to the preview.</summary>
    public void ShowResult(string output)
    {
        Status = Session.SourceWindow == 0
            ? UiStrings.Get("ReviewResultCopyMessage")
            : UiStrings.Get("ReviewResultReplaceMessage");
        OnPropertyChanged(nameof(OutputText));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsTransforming));
        OnPropertyChanged(nameof(CanUseResult));
        OnPropertyChanged(nameof(CanReplaceResult));
        OnPropertyChanged(nameof(CanProcess));
        ReplaceCommand.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
        ProcessCommand.NotifyCanExecuteChanged();
        InstructionsCommand.NotifyCanExecuteChanged();
        NewCommand.NotifyCanExecuteChanged();
        FreeCommand.NotifyCanExecuteChanged();
        SelectPresetCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Clears the current result and prepares a safe manual-input session.</summary>
    public void ResetForNewInput()
    {
        Session.ResetForNewInput();
        inputText = string.Empty;
        UpdateDetectedLanguage();
        selectedAction = Actions.FirstOrDefault();
        Session.ActionId = selectedAction?.Id;
        Status = UiStrings.Get("EnterTextProcessMessage");
        OnPropertyChanged(nameof(InputText));
        OnPropertyChanged(nameof(SelectedOutputLanguage));
        OnPropertyChanged(nameof(SelectedAction));
        OnPropertyChanged(nameof(OutputText));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsTransforming));
        OnPropertyChanged(nameof(CanUseResult));
        OnPropertyChanged(nameof(CanReplaceResult));
        OnPropertyChanged(nameof(CanProcess));
        ReplaceCommand.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
        ProcessCommand.NotifyCanExecuteChanged();
        InstructionsCommand.NotifyCanExecuteChanged();
        NewCommand.NotifyCanExecuteChanged();
        FreeCommand.NotifyCanExecuteChanged();
        SelectPresetCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Displays an isolated Free input in the session without retaining a replacement target.</summary>
    public void SetFreeInput(string input)
    {
        Session.StartFreeInput(input);
        inputText = input;
        UpdateDetectedLanguage();
        OnPropertyChanged(nameof(InputText));
        OnPropertyChanged(nameof(SelectedOutputLanguage));
        OnPropertyChanged(nameof(DetectedLanguageName));
        OnPropertyChanged(nameof(OutputText));
        OnPropertyChanged(nameof(CanUseResult));
        OnPropertyChanged(nameof(CanReplaceResult));
        OnPropertyChanged(nameof(CanProcess));
        CopyCommand.NotifyCanExecuteChanged();
        ReplaceCommand.NotifyCanExecuteChanged();
        ProcessCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Updates controls for a processing request started by the user.</summary>
    public void ShowTransforming(string message)
    {
        Status = message;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(OutputText));
        OnPropertyChanged(nameof(IsTransforming));
        OnPropertyChanged(nameof(CanUseResult));
        OnPropertyChanged(nameof(CanReplaceResult));
        OnPropertyChanged(nameof(CanProcess));
        ReplaceCommand.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
        ProcessCommand.NotifyCanExecuteChanged();
        FreeCommand.NotifyCanExecuteChanged();
        SelectPresetCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Updates the visible Full log warning when the Settings toggles change.</summary>
    public void SetFullLogActive(bool active) => IsFullLogActive = active;

    /// <summary>Refreshes the output-language selection after automatic translation routing.</summary>
    public void RefreshOutputLanguage() => OnPropertyChanged(nameof(SelectedOutputLanguage));

    /// <summary>Updates the locale-cache warning after a cache or UI-language preference changes.</summary>
    public void SetUiTranslationRequired(bool required) => IsUiTranslationRequired = required;

    /// <summary>Shows the connection category that was actually selected for the current invocation.</summary>
    public void SetActiveConnection(ConnectionCategory category)
    {
        if (activeConnectionCategory == category) return;
        activeConnectionCategory = category;
        OnPropertyChanged(nameof(HasActiveConnection));
        OnPropertyChanged(nameof(ActiveConnectionCategory));
        OnPropertyChanged(nameof(ActiveConnectionSymbol));
        OnPropertyChanged(nameof(ActiveConnectionLabel));
        OnPropertyChanged(nameof(ActiveConnectionActivityLabel));
    }

    private ActionDefinition? FindAction(string? actionId) => Actions.FirstOrDefault(action => action.Id.Equals(actionId, StringComparison.OrdinalIgnoreCase));

    private void UpdateDetectedLanguage()
    {
        if (Session.IsDetectingInputLanguage)
        {
            DetectedLanguageName = UiStrings.Get("DetectingInputLanguageLabel");
            return;
        }
        if (Session.InputLanguageDetectionFailed)
        {
            DetectedLanguageName = UiStrings.Get("InputLanguageDetectionFailedLabel");
            return;
        }
        string? languageCode = Session.DetectedInputLanguage;
        if (languageCode is null)
        {
            DetectedLanguageName = Session.IsInputLanguageDetectionComplete ? UiStrings.Get("InputLanguageUndeterminedLabel") : string.Empty;
            return;
        }
        try
        {
            DetectedLanguageName = CultureInfo.GetCultureInfo(languageCode).EnglishName;
        }
        catch (CultureNotFoundException)
        {
            DetectedLanguageName = languageCode;
        }
    }

    private static IReadOnlyList<LanguageOption> CreateOutputLanguages()
    {
        string unchanged = UiStrings.Get("UnchangedOutputLanguageLabel");
        return [new LanguageOption("Unchanged", unchanged, unchanged, unchanged), .. LanguageCatalog.Supported];
    }
}

/// <summary>Represents one user-configurable one-click action preset.</summary>
public sealed class ActionPreset(int slot, ActionDefinition? action) : ObservableObject
{
    private ActionDefinition? assignedAction = action;
    public int Slot { get; } = slot;
    public ActionDefinition? Action
    {
        get => assignedAction;
        set
        {
            if (!SetProperty(ref assignedAction, value)) return;
            OnPropertyChanged(nameof(Label));
        }
    }
    public string Label => Action?.DisplayName ?? UiStrings.Get("UnassignedActionLabel");

    /// <summary>Notifies the view that the action label was resolved again for a new locale.</summary>
    public void RefreshLabel() => OnPropertyChanged(nameof(Label));
}
