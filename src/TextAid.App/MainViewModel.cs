using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextAid.Core;

namespace TextAid.App;

/// <summary>Exposes one short-lived result transaction to the session window.</summary>
public sealed class MainViewModel : ObservableObject
{
    public MainViewModel(InvocationSession session, Action replace, Action copy, Action close, string status)
    {
        Session = session;
        Status = status;
        ReplaceCommand = new RelayCommand(replace, () => CanUseResult);
        CopyCommand = new RelayCommand(copy, () => CanUseResult);
        CancelCommand = new RelayCommand(close);
    }

    public InvocationSession Session { get; }
    public string InputText => Session.InputText;
    public string? OutputText => Session.OutputText;
    public string Status { get; private set; }
    public bool CanUseResult => Session.State == InvocationState.ResultReady && !string.IsNullOrEmpty(Session.OutputText);
    public IRelayCommand ReplaceCommand { get; }
    public IRelayCommand CopyCommand { get; }
    public IRelayCommand CancelCommand { get; }

    /// <summary>Disables result actions and shows a safe-failure message.</summary>
    public void ShowFailure(string message)
    {
        Session.State = InvocationState.Failed;
        Status = message;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(CanUseResult));
        ReplaceCommand.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
    }
}
