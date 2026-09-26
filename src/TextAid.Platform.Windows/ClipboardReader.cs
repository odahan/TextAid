using System.Windows.Forms;

namespace TextAid.Platform.Windows;

/// <summary>Reads only Unicode plain text with bounded, cancellable retries.</summary>
public static class ClipboardReader
{
    public static async Task<string?> ReadUnicodeTextAsync(CancellationToken cancellationToken)
    {
        int[] delays = [0, 20, 40, 80, 160];
        foreach (int delay in delays)
        {
            if (delay > 0) await Task.Delay(delay, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                {
                    return Clipboard.GetText(TextDataFormat.UnicodeText);
                }
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                if (delay == delays[^1]) throw new InvalidOperationException("Unable to access the clipboard.");
            }
        }
        return null;
    }
}
