using TextAid.AI;
using TextAid.Core;

namespace TextAid.AI.Tests;

public sealed class StartupModelWarmupTests
{
    private static readonly TextTransformationSettings Settings = new("http://localhost:11434", "test-model", 0.2f, TimeSpan.FromSeconds(120), 16384, ThinkingMode.Off);

    [Fact]
    public async Task Preload_DelaysEachAttemptAndRetriesUnavailableOllamaOnlyUntilSuccess()
    {
        int probes = 0;
        int preloads = 0;
        var delays = new List<TimeSpan>();
        var events = new List<string>();
        var service = new StartupModelWarmup(() => Settings,
            (_, _) => ++probes switch
            {
                1 => throw new HttpRequestException("Not ready"),
                2 => Task.FromResult(false),
                _ => Task.FromResult(true)
            },
            (settings, _) => { Assert.Equal(16384, settings.ContextSize); preloads++; return Task.CompletedTask; },
            events.Add,
            (duration, _) => { delays.Add(duration); Assert.Equal(delays.Count - 1, probes); return Task.CompletedTask; });
        await service.RunAsync(CancellationToken.None);
        Assert.Equal(3, probes);
        Assert.Equal(1, preloads);
        Assert.All(delays, duration => Assert.Equal(TimeSpan.FromSeconds(30), duration));
        Assert.Equal("startup-model-warmup-completed", events[^1]);
    }

    [Fact]
    public async Task Preload_DoesNotContactOllamaWhenTheLocalProfileIsDisabledOrUnconfigured()
    {
        var service = new StartupModelWarmup(() => null,
            (_, _) => throw new InvalidOperationException("Unexpected probe"),
            (_, _) => throw new InvalidOperationException("Unexpected preload"), _ => { },
            (_, _) => Task.CompletedTask);
        await service.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Preload_StopsWhenTheApplicationExitsDuringTheInitialDelay()
    {
        using var cancellation = new CancellationTokenSource();
        var service = new StartupModelWarmup(() => Settings,
            (_, _) => throw new InvalidOperationException("Unexpected probe"),
            (_, _) => throw new InvalidOperationException("Unexpected preload"), _ => { },
            (_, token) => { cancellation.Cancel(); return Task.Delay(1, token); });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RunAsync(cancellation.Token));
    }
}
