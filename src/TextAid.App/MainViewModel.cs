using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TextAid.Core;

namespace TextAid.App;

/// <summary>Exposes one short-lived result transaction to the session window.</summary>
public sealed class MainViewModel : ObservableObject
{
    private string inputText;
    private ActionDefinition? selectedAction;
    private readonly Action processPreset;

    public MainViewModel(InvocationSession session, IReadOnlyList<ActionDefinition> actions, IReadOnlyList<string> presetActionIds, Action replace, Action copy, Action process, Action instructions, Action reset, Action close, string status)
    {
        Session = session;
        inputText = session.InputText;
        Actions = new ObservableCollection<ActionDefinition>(actions);
        Presets = new ObservableCollection<ActionPreset>(Enumerable.Range(1, 4).Select(slot => new ActionPreset(slot, FindAction(presetActionIds.ElementAtOrDefault(slot - 1)))));
        selectedAction = Actions.FirstOrDefault(action => action.Id.Equals(session.ActionId, StringComparison.OrdinalIgnoreCase)) ?? Actions.FirstOrDefault();
        Session.ActionId = selectedAction?.Id;
        Status = status;
        processPreset = process;
        ReplaceCommand = new RelayCommand(replace, () => CanReplaceResult);
        CopyCommand = new RelayCommand(copy, () => CanUseResult);
        ProcessCommand = new RelayCommand(process, () => CanProcess);
        InstructionsCommand = new RelayCommand(instructions, () => !IsTransforming);
        NewCommand = new RelayCommand(reset, () => !IsTransforming);
        CancelCommand = new RelayCommand(close);
        SelectPresetCommand = new RelayCommand<ActionPreset>(SelectPreset, preset => preset?.Action is not null && !IsTransforming);
    }

    public InvocationSession Session { get; }
    public ObservableCollection<ActionDefinition> Actions { get; }
    public ObservableCollection<ActionPreset> Presets { get; }
    public ActionDefinition? SelectedAction
    {
        get => selectedAction;
        set
        {
            if (!SetProperty(ref selectedAction, value)) return;
            Session.ActionId = value?.Id;
            OnPropertyChanged(nameof(CanProcess));
            ProcessCommand.NotifyCanExecuteChanged();
        }
    }
    public string InputText
    {
        get => inputText;
        set
        {
            if (!SetProperty(ref inputText, value)) return;
            Session.InputText = value;
            OnPropertyChanged(nameof(CanProcess));
            ProcessCommand.NotifyCanExecuteChanged();
        }
    }
    public string? OutputText => Session.OutputText;
    public string Status { get; private set; }
    public bool IsTransforming => Session.State == InvocationState.Transforming;
    public bool CanUseResult => Session.State == InvocationState.ResultReady && !string.IsNullOrEmpty(Session.OutputText);
    public bool CanReplaceResult => CanUseResult && Session.SourceWindow != 0;
    public bool CanProcess => !IsTransforming && !string.IsNullOrWhiteSpace(InputText) && SelectedAction is not null;
    public IRelayCommand ReplaceCommand { get; }
    public IRelayCommand CopyCommand { get; }
    public IRelayCommand ProcessCommand { get; }
    public IRelayCommand InstructionsCommand { get; }
    public IRelayCommand NewCommand { get; }
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
        SelectPresetCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Publishes the completed local transformation to the preview.</summary>
    public void ShowResult(string output)
    {
        Session.OutputText = output;
        Session.State = InvocationState.ResultReady;
        Status = Session.SourceWindow == 0
            ? "Review the local transformation, then choose Copy or Cancel."
            : "Review the local transformation, then choose Replace, Copy, or Cancel.";
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
        SelectPresetCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Clears the current result and prepares a safe manual-input session.</summary>
    public void ResetForNewInput()
    {
        Session.ResetForNewInput();
        inputText = string.Empty;
        selectedAction = Actions.FirstOrDefault();
        Session.ActionId = selectedAction?.Id;
        Status = "Enter or paste text, then choose Process.";
        OnPropertyChanged(nameof(InputText));
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
        SelectPresetCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Updates controls for a processing request started by the user.</summary>
    public void ShowTransforming(string message)
    {
        Status = message;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsTransforming));
        OnPropertyChanged(nameof(CanProcess));
        ProcessCommand.NotifyCanExecuteChanged();
        SelectPresetCommand.NotifyCanExecuteChanged();
    }

    private ActionDefinition? FindAction(string? actionId) => Actions.FirstOrDefault(action => action.Id.Equals(actionId, StringComparison.OrdinalIgnoreCase));
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
    public string Label => Action?.DisplayName ?? "Unassigned";
}
