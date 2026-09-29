using System.Windows;
using System.Windows.Documents;
using TextAid.App;

namespace TextAid.Platform.Windows.Tests;

public sealed class MarkdownPreviewRendererTests
{
    [Fact]
    public void MarkdownPreview_RendersSupportedConstructsAsAnInertFlowDocument()
    {
        RunInSta(() =>
        {
            const string markdown = "# Heading\n\nParagraph with *emphasis*, **strong**, and `code`.\n\n- First\n- Second\n\n1. One\n2. Two\n\n> Quotation\n\n```text\nblock\n```\n\n---\n\n[TextAid](https://example.test)\n\n![remote image](https://example.test/image.png)\n\n<script>untrusted</script>";

            Assert.True(MarkdownPreviewRenderer.TryCreateDocument(markdown, out FlowDocument? document));
            Assert.NotNull(document);
            Assert.Contains("Segoe UI Variable Text", document!.FontFamily.Source, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(document!.Blocks, block => block is List);
            Assert.Contains(AllElements(document), element => element is Bold);
            Assert.Contains(AllElements(document), element => element is Italic);
            Assert.Contains(AllElements(document), element => element is Hyperlink);
            Assert.DoesNotContain(AllElements(document), element => element is InlineUIContainer);

            var text = new TextRange(document.ContentStart, document.ContentEnd).Text;
            Assert.Contains("[Image omitted: remote image]", text);
            Assert.DoesNotContain("<script", text, StringComparison.OrdinalIgnoreCase);

            foreach (Hyperlink link in AllElements(document).OfType<Hyperlink>())
            {
                Assert.Null(link.NavigateUri);
                Assert.Null(link.Command);
                Assert.False(link.IsEnabled);
            }
        });
    }

    [Fact]
    public void MarkdownPreview_FallsBackWhenRenderingFails()
    {
        RunInSta(() => Assert.False(MarkdownPreviewRenderer.TryCreateDocument(null!, out FlowDocument? document)));
    }

    private static IEnumerable<TextElement> AllElements(FlowDocument document)
    {
        foreach (Block block in document.Blocks)
        {
            foreach (TextElement element in AllElements(block)) yield return element;
        }
    }

    private static IEnumerable<TextElement> AllElements(Block block)
    {
        yield return block;
        if (block is Paragraph paragraph)
        {
            foreach (Inline inline in paragraph.Inlines)
            {
                foreach (TextElement element in AllElements(inline)) yield return element;
            }
        }
        else if (block is Section section)
        {
            foreach (Block child in section.Blocks)
            {
                foreach (TextElement element in AllElements(child)) yield return element;
            }
        }
        else if (block is List list)
        {
            foreach (ListItem item in list.ListItems)
            {
                yield return item;
                foreach (Block child in item.Blocks)
                {
                    foreach (TextElement element in AllElements(child)) yield return element;
                }
            }
        }
    }

    private static IEnumerable<TextElement> AllElements(Inline inline)
    {
        yield return inline;
        if (inline is Span span)
        {
            foreach (Inline child in span.Inlines)
            {
                foreach (TextElement element in AllElements(child)) yield return element;
            }
        }
    }

    private static void RunInSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) throw new Xunit.Sdk.XunitException(failure.ToString());
    }
}
