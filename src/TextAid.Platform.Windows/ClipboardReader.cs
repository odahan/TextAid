using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TextAid.Platform.Windows;

/// <summary>Reads only Unicode plain text with bounded, cancellable retries.</summary>
public static class ClipboardReader
{
    /// <summary>Reads the clipboard on a background STA so clipboard access cannot block the UI.</summary>
    public static Task<string?> ReadUnicodeTextAsync(uint expectedSequence, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                completion.SetResult(ReadUnicodeText(expectedSequence, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                completion.SetCanceled(cancellationToken);
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private static string? ReadUnicodeText(uint expectedSequence, CancellationToken cancellationToken)
    {
        int[] delays = [0, 20, 40, 80, 160];
        foreach (int delay in delays)
        {
            if (delay > 0) Thread.Sleep(delay);
            cancellationToken.ThrowIfCancellationRequested();
            if (GetClipboardSequenceNumber() != expectedSequence) return null;
            try
            {
                IDataObject? data = Clipboard.GetDataObject();
                if (GetClipboardSequenceNumber() != expectedSequence) return null;
                if (data?.GetData(DataFormats.UnicodeText, autoConvert: false) is string text) return text;
            }
            catch (ExternalException)
            {
                if (delay == delays[^1]) throw new InvalidOperationException("Unable to access the clipboard.");
            }
        }
        return null;
    }

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();
}
