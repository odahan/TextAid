using TextAid.Core;

namespace TextAid.Core.Tests;

public sealed class InputLanguageDetectionTests
{
    [Fact]
    public async Task Detection_IsSharedWithProcessingAndReusedAcrossActions()
    {
        using var session = new InvocationSession(0, "Il pleut, c'est la nuit, il marche sous la pluie. Il vient de nulle part, nous ne savons pas où il va...");
        var response = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        int requests = 0;
        Task<string?> Detect(string input, CancellationToken token)
        {
            requests++;
            Assert.Equal(session.InputText, input);
            return response.Task;
        }

        Task<string?> captured = session.DetectInputLanguageAsync(Detect);
        session.StartNewGeneration();
        Task<string?> processing = session.DetectInputLanguageAsync(Detect);
        Assert.Same(captured, processing);
        Assert.True(session.IsDetectingInputLanguage);
        response.SetResult("fr");
        Assert.Equal("fr", await processing);
        Assert.Equal("en", QuickTranslationRouting.SelectDestination(session.DetectedInputLanguage, "fr-FR", "en"));

        session.InvalidateResult();
        session.ActionId = "another-action";
        Assert.Equal("fr", await session.DetectInputLanguageAsync(Detect));
        Assert.Equal(1, requests);
        Assert.True(session.IsInputLanguageDetectionComplete);
        Assert.False(session.IsDetectingInputLanguage);
    }

    [Fact]
    public async Task ChangedInput_CancelsDetectionAndRejectsAStaleResponseEvenWhenTextIsRestored()
    {
        using var session = new InvocationSession(0, "original");
        var oldResponse = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken oldToken = default;
        Task<string?> pending = session.DetectInputLanguageAsync((_, token) =>
        {
            oldToken = token;
            return oldResponse.Task;
        });

        session.SetInputText("replacement");
        session.SetInputText("original");
        Assert.True(oldToken.IsCancellationRequested);
        Assert.Equal("ja", await session.DetectInputLanguageAsync((_, _) => Task.FromResult<string?>("ja")));
        oldResponse.SetResult("fr");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal("ja", session.DetectedInputLanguage);
        Assert.True(session.IsInputLanguageDetectionComplete);
        Assert.False(session.InputLanguageDetectionFailed);
    }

    [Fact]
    public async Task FailedDetection_CanBeRetriedWithoutChangingInput()
    {
        using var session = new InvocationSession(0, "captured text");
        await Assert.ThrowsAsync<HttpRequestException>(() => session.DetectInputLanguageAsync((_, _) => throw new HttpRequestException()));
        Assert.True(session.InputLanguageDetectionFailed);
        Assert.False(session.IsDetectingInputLanguage);

        Assert.Equal("fr", await session.DetectInputLanguageAsync((_, _) => Task.FromResult<string?>("fr")));
        Assert.False(session.InputLanguageDetectionFailed);
    }

    [Fact]
    public async Task IndeterminateDetection_IsCachedWithoutChoosingATranslationDirection()
    {
        using var session = new InvocationSession(0, "ambiguous");
        Assert.Null(await session.DetectInputLanguageAsync((_, _) => Task.FromResult<string?>(null)));
        Assert.Null(await session.DetectInputLanguageAsync((_, _) => throw new InvalidOperationException("Must use the cached response.")));
        Assert.True(session.IsInputLanguageDetectionComplete);
        Assert.Throws<ArgumentNullException>(() => QuickTranslationRouting.SelectDestination(session.DetectedInputLanguage, "fr", "en"));
    }

    [Fact]
    public async Task ClosingSession_CancelsThePendingDetection()
    {
        var session = new InvocationSession(0, "captured text");
        var response = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken token = default;
        Task<string?> pending = session.DetectInputLanguageAsync((_, cancellation) =>
        {
            token = cancellation;
            return response.Task;
        });
        session.Dispose();
        Assert.True(token.IsCancellationRequested);
        response.SetResult("fr");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Null(session.DetectedInputLanguage);
    }

    [Fact]
    public void ChangedInput_ResetsAnAutomaticDestinationButPreservesAnExplicitChoice()
    {
        using var session = new InvocationSession(0, "English input");
        session.SelectAutomaticTranslationDestination("en", "fr", "en");
        Assert.Equal("fr", session.OutputLanguage);
        session.SetInputText("Texte français");
        Assert.Equal("Unchanged", session.OutputLanguage);
        session.SelectAutomaticTranslationDestination("fr", "fr", "en");
        Assert.Equal("en", session.OutputLanguage);
        session.OutputLanguage = "de";
        session.SetInputText("Other input");
        Assert.Equal("de", session.OutputLanguage);
    }
}
