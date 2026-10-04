using System.Windows.Threading;
using TextAid.App.ViewModels;
using TextAid.Core;

namespace TextAid.Platform.Windows.Tests;

public sealed class InputLanguageViewModelTests
{
    [Fact]
    public void BackgroundDetection_KeepsTheUiResponsiveAndPublishesItsResultOnTheUiThread()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                using var session = new InvocationSession(0, "Captured text");
                var viewModel = new MainViewModel(session, BuiltInActionCatalog.Create(), [],
                    () => { }, () => { }, () => { }, () => { }, () => { }, () => { }, () => { }, "Ready", false, false);
                var providerResponse = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
                bool uiTurnRan = false;
                bool updateOnUi = false;
                viewModel.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(MainViewModel.DetectedLanguageName) && session.IsInputLanguageDetectionComplete)
                    {
                        updateOnUi = dispatcher.CheckAccess();
                    }
                };
                Task<string?> detection = session.DetectInputLanguageAsync((_, token) => Task.Run(async () =>
                {
                    Assert.False(dispatcher.CheckAccess());
                    return await providerResponse.Task.WaitAsync(token);
                }, token));
                Assert.False(detection.IsCompleted);
                Assert.True(viewModel.CanProcess);

                var frame = new DispatcherFrame();
                dispatcher.BeginInvoke(() =>
                {
                    uiTurnRan = true;
                    providerResponse.SetResult("sv");
                });
                detection.ContinueWith(_ => dispatcher.BeginInvoke(() => frame.Continue = false), TaskScheduler.Default);
                Dispatcher.PushFrame(frame);

                Assert.Equal("sv", detection.GetAwaiter().GetResult());
                Assert.True(uiTurnRan);
                Assert.True(updateOnUi);
                Assert.Equal("Swedish", viewModel.DetectedLanguageName);
                session.SetInputText("Changed text");
                Assert.Equal(string.Empty, viewModel.DetectedLanguageName);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "The UI test did not finish.");
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
