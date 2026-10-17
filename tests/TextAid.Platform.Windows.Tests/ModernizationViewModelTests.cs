using TextAid.App.ViewModels;
using TextAid.Core;

namespace TextAid.Platform.Windows.Tests;

public sealed class ModernizationViewModelTests
{
    [Fact]
    public void Transfer_PerformsNoResultActionOrProcessingAndPreservesTarget()
    {
        using var session = new InvocationSession(123, "input");
        int externalCalls = 0;
        var vm = new MainViewModel(session, BuiltInActionCatalog.Create(), [],
            () => externalCalls++, () => externalCalls++, () => externalCalls++, () => externalCalls++, () => externalCalls++, () => externalCalls++, () => externalCalls++, "Ready", false, false);
        int generation = session.StartNewGeneration();
        Assert.False(vm.TransferResultCommand.CanExecute(null));
        session.TryCompleteGeneration(generation, "# raw\n", "fr");
        vm.ShowResult(session.OutputText!);
        Assert.True(vm.TransferResultCommand.CanExecute(null));
        vm.TransferResultCommand.Execute(null);
        Assert.Equal("# raw\n", vm.InputText);
        Assert.Equal("fr", session.DetectedInputLanguage);
        Assert.Equal(123, session.SourceWindow);
        Assert.Equal(0, externalCalls);
        Assert.False(vm.CopyCommand.CanExecute(null));
        Assert.False(vm.ReplaceCommand.CanExecute(null));
        Assert.False(vm.TransferResultCommand.CanExecute(null));
    }

    [Fact]
    public void Selector_RefreshResetAndRunningSnapshot_DoNotEraseResultOrProvider()
    {
        using var session = new InvocationSession(0, "input");
        int calls = 0;
        var vm = new MainViewModel(session, BuiltInActionCatalog.Create(), [], () => { }, () => { }, () => calls++, () => { }, () => { }, () => { }, () => { }, "Ready", false, false);
        var local = new ConnectionDefinition("local", ConnectionCategory.ThisDeviceOnly, "ollama", "http://127.0.0.1:11434", true, AuthenticationKind.None, null);
        var external = new ConnectionDefinition("external", ConnectionCategory.External, "openai-compatible", "https://example.com", true, AuthenticationKind.None, null);
        var first = new ModelProfile("one", "local", "same-name", 0.2f, TimeSpan.FromMinutes(2), new Dictionary<string, object?>());
        var second = first with { Id = "two", ConnectionId = "external" };
        var config = new ConfigurationSnapshot(1, [local, external], [first], "en", "fr", "Ctrl+C+C", "Ctrl+C+T");
        vm.RefreshModelChoices(config, new EmptyVault());
        Assert.False(vm.ShowModelSelector);
        config = config with { Profiles = [first, second] };
        vm.RefreshModelChoices(config, new EmptyVault());
        Assert.True(vm.ShowModelSelector);
        Assert.Null(vm.SelectedModelChoice?.ProfileId);
        vm.SelectedModelChoice = vm.ModelChoices[2];
        int generation = session.StartNewGeneration();
        Assert.Equal("two", session.RunningProfileId);
        vm.SelectedModelChoice = vm.ModelChoices[1];
        Assert.Equal("two", session.RunningProfileId);
        vm.SetActiveConnection(external, second);
        session.TryCompleteGeneration(generation, "result", "en");
        vm.ShowResult("result");
        Assert.Null(vm.SelectedModelChoice?.ProfileId);
        Assert.True(session.HasCurrentResult);
        Assert.Contains("same-name", vm.ActiveConnectionLabel);
        Assert.Equal("◎", vm.ActiveConnectionSymbol);
        Assert.Equal(0, calls);
        vm.SelectedModelChoice = vm.ModelChoices[1];
        vm.ShowFailure("failure");
        Assert.Null(session.NextProfileId);
        config = config with { Profiles = [first] };
        vm.RefreshModelChoices(config, new EmptyVault());
        Assert.False(vm.ShowModelSelector);
        Assert.Null(session.NextProfileId);
    }

    private sealed class EmptyVault : ISecretVault
    {
        public void SetSecret(string reference, string secret) { }
        public bool TryGetSecret(string reference, out string secret) { secret = ""; return false; }
        public void RemoveSecret(string reference) { }
    }
}
