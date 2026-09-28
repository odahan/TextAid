using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextAid.Core;
using TextAid.App.Localization;

namespace TextAid.App.ViewModels;

/// <summary>Edits the visible declarative action store without changing invalid active data.</summary>
public sealed partial class ActionsViewModel : ObservableObject
{
    private readonly ActionLoader loader;
    [ObservableProperty] private EditableAction? selectedAction;
    [ObservableProperty] private string status = UiStrings.Get("ActionsEditorHelp");
    public event EventHandler? ActionsSaved;
    public event Func<string, bool>? DeleteConfirmationRequested;

    public ActionsViewModel()
    {
        ConfigurationSnapshot configuration = UserConfiguration.LoadConfiguration();
        loader = new ActionLoader(configuration.Profiles.Select(profile => profile.Id), UserConfiguration.EnsureActionsDirectory());
        Actions = new ObservableCollection<EditableAction>(loader.LoadAll().Select(EditableAction.From));
        Profiles = configuration.Profiles.Select(profile => profile.Id).ToArray();
        string unchanged = UiStrings.Get("UnchangedOutputLanguageLabel");
        OutputLanguages = [new LanguageOption("Unchanged", unchanged, unchanged, unchanged), .. LanguageCatalog.Supported];
        SelectedAction = Actions.FirstOrDefault();
    }

    public ObservableCollection<EditableAction> Actions { get; }
    public IReadOnlyList<string> Profiles { get; }
    public IReadOnlyList<LanguageOption> OutputLanguages { get; }

    /// <summary>Refreshes built-in action labels after the active UI locale changes.</summary>
    public void RefreshLocalizedLabels()
    {
        foreach (EditableAction action in Actions) action.RefreshLocalizedLabel();
    }

    [RelayCommand]
    private void NewAction()
    {
        var action = new EditableAction($"custom-{Guid.NewGuid():N}"[..15], "", true, Profiles.First(), 0.2f, "Unchanged", false, "Process this text:\n\n{{text}}", false);
        Actions.Add(action);
        SelectedAction = action;
        Status = UiStrings.Get("NewActionHelp");
    }

    /// <summary>Reloads the action list only when the complete stored set is valid.</summary>
    [RelayCommand]
    private void ReloadActions()
    {
        try
        {
            IReadOnlyList<EditableAction> reloaded = loader.LoadAll().Select(EditableAction.From).ToArray();
            Actions.Clear();
            foreach (EditableAction action in reloaded) Actions.Add(action);
            SelectedAction = Actions.FirstOrDefault();
            Status = UiStrings.Get("ActionsReloadedMessage");
            ActionsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            Status = UiStrings.Get("ActionsReloadFailureMessage");
        }
    }

    /// <summary>Removes the selected user-editable action after explicit confirmation.</summary>
    [RelayCommand]
    private void DeleteAction()
    {
        if (SelectedAction is null) return;
        if (SelectedAction.IsReserved) { Status = UiStrings.Get("ReservedTranslateDeleteError"); return; }
        if (DeleteConfirmationRequested?.Invoke(SelectedAction.Label) != true) return;
        try
        {
            loader.Delete(SelectedAction.Id);
            Actions.Remove(SelectedAction);
            SelectedAction = Actions.FirstOrDefault();
            ActionsSaved?.Invoke(this, EventArgs.Empty);
            Status = UiStrings.Get("ActionDeletedMessage");
        }
        catch (Exception) { Status = UiStrings.Get("ActionOperationFailureMessage"); }
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedAction is null) return;
        try
        {
            loader.Save(SelectedAction.ToDefinition());
            ActionsSaved?.Invoke(this, EventArgs.Empty);
            Status = UiStrings.Get("ActionSavedMessage");
        }
        catch (Exception)
        {
            Status = UiStrings.Get("ActionOperationFailureMessage");
        }
    }

    /// <summary>Opens the persisted active action folder for backup or inspection.</summary>
    [RelayCommand]
    private void OpenActionsFolder()
    {
        Process.Start(new ProcessStartInfo { FileName = loader.DirectoryPath, UseShellExecute = true });
    }
}

/// <summary>Provides editable action fields for the WPF action page.</summary>
public sealed partial class EditableAction : ObservableObject
{
    [ObservableProperty] private string id;
    [ObservableProperty] private string displayNameOverride;
    [ObservableProperty] private bool isEnabled;
    [ObservableProperty] private string profileId;
    [ObservableProperty] private float? temperatureOverride;
    [ObservableProperty] private string outputLanguageDefault;
    [ObservableProperty] private bool askForUserInstructions;
    [ObservableProperty] private string promptTemplate;
    [ObservableProperty] private string userInstructionsQuestion;
    public bool IsReserved { get; }
    /// <summary>Indicates whether fields protected for the immediate Translate action may be changed.</summary>
    public bool IsEditable => !IsReserved;

    public EditableAction(string id, string displayNameOverride, bool isEnabled, string profileId, float? temperatureOverride, string outputLanguageDefault, bool askForUserInstructions, string promptTemplate, bool isReserved, string userInstructionsQuestion = "")
    { this.id = id; this.displayNameOverride = displayNameOverride; this.isEnabled = isEnabled; this.profileId = profileId; this.temperatureOverride = temperatureOverride; this.outputLanguageDefault = outputLanguageDefault; this.askForUserInstructions = askForUserInstructions; this.promptTemplate = promptTemplate; IsReserved = isReserved; this.userInstructionsQuestion = userInstructionsQuestion; }

    public string Label => string.IsNullOrWhiteSpace(DisplayNameOverride) ? BuiltInActionCatalog.GetDisplayName(Id) : DisplayNameOverride;
    /// <summary>Raises a label update after the application locale changes.</summary>
    public void RefreshLocalizedLabel() => OnPropertyChanged(nameof(Label));
    public static EditableAction From(ActionDefinition action) => new(action.Id, action.DisplayNameOverride ?? "", action.IsEnabled, action.ProfileId, action.TemperatureOverride, action.OutputLanguageDefault, action.AskForUserInstructions, action.PromptTemplate, action.IsReserved, action.UserInstructionsQuestion ?? "");
    public ActionDefinition ToDefinition() => new(Id.Trim(), string.IsNullOrWhiteSpace(DisplayNameOverride) ? null : DisplayNameOverride.Trim(), IsEnabled, PromptTemplate, ProfileId, TemperatureOverride, OutputLanguageDefault, AskForUserInstructions, IsReserved, string.IsNullOrWhiteSpace(UserInstructionsQuestion) ? null : UserInstructionsQuestion.Trim());
}
