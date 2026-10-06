using TextAid.App.ViewModels;
using TextAid.Core;

namespace TextAid.Platform.Windows.Tests;

public sealed class StreamingPreviewTests
{
    [Fact]
    public void PartialOutput_CannotBeCopiedOrReplacedAndDisappearsAfterFailure()
    {
        using var session = new InvocationSession(123, "Bonjour");
        var viewModel = CreateViewModel(session);
        session.StartNewGeneration();
        viewModel.ShowTransforming("Working");
        viewModel.ShowPartialResult("Hello");

        Assert.Equal("Hello", viewModel.OutputText);
        Assert.Null(session.OutputText);
        Assert.False(viewModel.CopyCommand.CanExecute(null));
        Assert.False(viewModel.ReplaceCommand.CanExecute(null));

        viewModel.ShowFailure("Timeout");
        Assert.Null(viewModel.OutputText);
        Assert.False(viewModel.CopyCommand.CanExecute(null));
        viewModel.ShowPartialResult("Stale update");
        Assert.Null(viewModel.OutputText);
    }

    [Fact]
    public void CompletedOutput_ReplacesThePreviewAndEnablesResultActions()
    {
        using var session = new InvocationSession(123, "Bonjour");
        var viewModel = CreateViewModel(session);
        int generation = session.StartNewGeneration();
        viewModel.ShowTransforming("Working");
        viewModel.ShowPartialResult("Hel");
        Assert.True(session.TryCompleteGeneration(generation, "Hello"));
        viewModel.ShowResult("Hello");

        Assert.Equal("Hello", viewModel.OutputText);
        Assert.True(viewModel.CopyCommand.CanExecute(null));
        Assert.True(viewModel.ReplaceCommand.CanExecute(null));
    }

    private static MainViewModel CreateViewModel(InvocationSession session) => new(session, BuiltInActionCatalog.Create(), [],
        () => { }, () => { }, () => { }, () => { }, () => { }, () => { }, () => { }, "Ready", false, false);
}
