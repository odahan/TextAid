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

    public MainViewModel(InvocationSession session, IReadOnlyList<ActionDefinition> actions, Action replace, Action copy, Action process, Action instructions, Action reset, Action close, string status)
    {
        Session = session;
        inputText = session.InputText;
        Actions = new ObservableCollection<ActionDefinition>(actions);
        selectedAction = Actions.FirstOrDefault(action => action.Id.Equals(session.ActionId, StringComparison.OrdinalIgnoreCase)) ?? Actions.FirstOrDefault();
        Session.ActionId = selectedAction?.Id;
        Status = status;
        ReplaceCommand = new RelayCommand(replace, () => CanReplaceResult);
        CopyCommand = new RelayCommand(copy, () => CanUseResult);
        ProcessCommand = new RelayCommand(process, () => CanProcess);
        InstructionsCommand = new RelayCommand(instructions, () => !IsTransforming);
        NewCommand = new RelayCommand(reset, () => !IsTransforming);
        CancelCommand = new RelayCommand(close);
    }

    public InvocationSession Session { get; }
    public ObservableCollection<ActionDefinition> Actions { get; }
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
    }

    /// <summary>Updates controls for a processing request started by the user.</summary>
    public void ShowTransforming(string message)
    {
        Status = message;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsTransforming));
        OnPropertyChanged(nameof(CanProcess));
        ProcessCommand.NotifyCanExecuteChanged();
    }
}
