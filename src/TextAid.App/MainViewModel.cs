using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextAid.Core;

namespace TextAid.App;

/// <summary>Exposes one short-lived result transaction to the session window.</summary>
public sealed class MainViewModel : ObservableObject
{
    private string inputText;

    public MainViewModel(InvocationSession session, Action replace, Action copy, Action process, Action close, string status)
    {
        Session = session;
        inputText = session.InputText;
        Status = status;
        ReplaceCommand = new RelayCommand(replace, () => CanReplaceResult);
        CopyCommand = new RelayCommand(copy, () => CanUseResult);
        ProcessCommand = new RelayCommand(process, () => CanProcess);
        CancelCommand = new RelayCommand(close);
    }

    public InvocationSession Session { get; }
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
    public bool CanProcess => !IsTransforming && !string.IsNullOrWhiteSpace(InputText);
    public IRelayCommand ReplaceCommand { get; }
    public IRelayCommand CopyCommand { get; }
    public IRelayCommand ProcessCommand { get; }
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
