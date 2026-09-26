using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextAid.Core;

namespace TextAid.App;

/// <summary>Exposes one V0.1 invocation transaction to the session window.</summary>
public sealed class MainViewModel : ObservableObject
{
    public MainViewModel(InvocationSession session, Action close, string status)
    {
        Session = session;
        Status = status;
        CancelCommand = new RelayCommand(close);
    }

    public InvocationSession Session { get; }
    public string InputText => Session.InputText;
    public string Status { get; }
    public bool CanAccept => Session.State == InvocationState.ResultReady;
    public IRelayCommand CancelCommand { get; }
}
