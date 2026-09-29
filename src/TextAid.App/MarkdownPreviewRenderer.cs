using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using MdXaml;

namespace TextAid.App;

/// <summary>Creates inert, dark-theme FlowDocuments for Markdown result previews.</summary>
public static partial class MarkdownPreviewRenderer
{
    /// <summary>Attempts to render Markdown without changing the raw result used by result actions.</summary>
    public static bool TryCreateDocument(string markdown, out FlowDocument? document)
    {
        document = null;

        try
        {
            string previewMarkdown = HtmlTagPattern().Replace(ImagePattern().Replace(markdown, "[Image omitted: $1]"), string.Empty);
            var renderer = new Markdown
            {
                DisabledTag = true,
                DisabledLazyLoad = true,
                DisabledContextMenu = true,
                HyperlinkCommand = null,
                OnHyperLinkClicked = _ => { }
            };

            document = renderer.Transform(previewMarkdown);
            ApplyDarkTheme(document);
            DisableHyperlinks(document);
            return true;
        }
        catch (Exception)
        {
            document = null;
            return false;
        }
    }

    private static void ApplyDarkTheme(FlowDocument document)
    {
        Brush foreground = FindBrush("ForegroundBrush", Brushes.White);
        Brush surface = FindBrush("SurfaceBrush", Brushes.Black);
        Brush accent = FindBrush("AccentBrush", Brushes.DeepSkyBlue);

        document.Foreground = foreground;
        document.Background = surface;
        document.PagePadding = new Thickness(0);
        document.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
        document.FontSize = 14;
        document.LineHeight = 20;

        foreach (TextElement element in EnumerateTextElements(document))
        {
            element.Foreground = foreground;
            element.FontFamily = document.FontFamily;
            if (element is Span span && IsCodeSpan(span)) span.Background = FindBrush("SurfaceAltBrush", Brushes.DimGray);
            if (element is Hyperlink hyperlink)
            {
                hyperlink.Foreground = accent;
                hyperlink.TextDecorations = TextDecorations.Underline;
            }
        }
    }

    private static void DisableHyperlinks(FlowDocument document)
    {
        foreach (Hyperlink hyperlink in EnumerateTextElements(document).OfType<Hyperlink>())
        {
            hyperlink.NavigateUri = null;
            hyperlink.Command = null;
            hyperlink.IsEnabled = false;
        }
    }

    private static IEnumerable<TextElement> EnumerateTextElements(FlowDocument document)
    {
        foreach (Block block in document.Blocks)
        {
            foreach (TextElement element in EnumerateBlock(block)) yield return element;
        }
    }

    private static IEnumerable<TextElement> EnumerateBlock(Block block)
    {
        yield return block;

        switch (block)
        {
            case Section section:
                foreach (Block child in section.Blocks)
                {
                    foreach (TextElement element in EnumerateBlock(child)) yield return element;
                }
                break;
            case List list:
                foreach (ListItem item in list.ListItems)
                {
                    yield return item;
                    foreach (Block child in item.Blocks)
                    {
                        foreach (TextElement element in EnumerateBlock(child)) yield return element;
                    }
                }
                break;
            case Paragraph paragraph:
                foreach (Inline inline in paragraph.Inlines)
                {
                    foreach (TextElement element in EnumerateInline(inline)) yield return element;
                }
                break;
        }
    }

    private static IEnumerable<TextElement> EnumerateInline(Inline inline)
    {
        yield return inline;
        if (inline is Span span)
        {
            foreach (Inline child in span.Inlines)
            {
                foreach (TextElement element in EnumerateInline(child)) yield return element;
            }
        }
    }

    private static bool IsCodeSpan(Span span) => span.FontFamily.Source.Contains("Consolas", StringComparison.OrdinalIgnoreCase)
        || span.FontFamily.Source.Contains("Courier", StringComparison.OrdinalIgnoreCase);

    private static Brush FindBrush(string resourceKey, Brush fallback) => Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;

    [GeneratedRegex(@"!\[([^\]]*)\](?:\([^\r\n)]*\)|\[[^\]]*\])", RegexOptions.CultureInvariant)]
    private static partial Regex ImagePattern();

    [GeneratedRegex(@"</?[A-Za-z][^>\r\n]*>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagPattern();
}
