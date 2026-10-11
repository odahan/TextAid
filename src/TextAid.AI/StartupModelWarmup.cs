using TextAid.Core;

namespace TextAid.AI;

/// <summary>Delays model preload and retries connection availability without blocking application startup.</summary>
public sealed class StartupModelWarmup(
    Func<TextTransformationSettings?> getSettings,
    Func<TextTransformationSettings, CancellationToken, Task<bool>> isAvailable,
    Func<TextTransformationSettings, CancellationToken, Task> warmup,
    Action<string> reportEvent,
    Func<TimeSpan, CancellationToken, Task>? delay = null)
{
    /// <summary>Waits thirty seconds before each availability attempt and stops after one successful preload.</summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Func<TimeSpan, CancellationToken, Task> wait = delay ?? Task.Delay;
        while (true)
        {
            await wait(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            TextTransformationSettings? settings = getSettings();
            if (settings is null) return;
            bool available;
            using (var probe = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                probe.CancelAfter(TimeSpan.FromSeconds(5));
                try { available = await isAvailable(settings, probe.Token).ConfigureAwait(false); }
                catch (HttpRequestException) { available = false; }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { available = false; }
            }
            if (!available)
            {
                reportEvent("startup-model-warmup-waiting-for-ollama");
                continue;
            }
            reportEvent("startup-model-warmup-started");
            await warmup(settings, cancellationToken).ConfigureAwait(false);
            reportEvent("startup-model-warmup-completed");
            return;
        }
    }
}
