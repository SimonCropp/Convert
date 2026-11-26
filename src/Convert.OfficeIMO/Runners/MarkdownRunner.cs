using OfficeIMO.Html;
using OfficeIMO.Markdown;
using OfficeIMO.Markdown.Html;
using OfficeIMO.MarkdownRenderer;
using OfficeIMO.Word.Markdown;

/// <summary>Markdown to HTML and Word, and HTML to Markdown.</summary>
static class MarkdownRunner
{
    const int maxTextChars = 500_000;

    public static string ToHtml(string markdown)
    {
        Guard(markdown);
        var options = MarkdownRendererPresets.CreateStrictMinimalPortable();
        options.MarkdownOverflowHandling = OverflowHandling.Throw;
        return MarkdownRenderer.RenderBodyHtml(markdown, options);
    }

    public static (string Markdown, IReadOnlyList<string> Notes) FromHtml(string html)
    {
        Guard(html);
        var options = HtmlMarkdownExportOptions.CreatePortableProfile();
        options.FailOnLoss = false;
        var converted = HtmlConversionDocument.Parse(html).ToMarkdown(options, out var report);
        return (converted.ToMarkdown(), Reports.Notes(report));
    }

    /// <param name="drawMermaid">Whether Naiad was loaded for this file, to draw its Mermaid fences.</param>
    public static async Task<(byte[] Bytes, IReadOnlyList<string> Notes)> ToDocxAsync(string markdown, bool drawMermaid, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        Guard(markdown);
        var document = MarkdownDocument.Parse(markdown);
        var notes = new List<string>();
        if (drawMermaid)
        {
            await MermaidBlocks.DrawAsync(document, notes, progress, cancel);
        }

        progress.Report(new("Writing", "Word"));
        using var word = document.ToWord(
            new()
            {
                FailOnLoss = false,
                PreferNarrativeSingleLineDefinitions = true
            },
            cancel);
        return (word.ToBytes(), notes);
    }

    static void Guard(string text)
    {
        if (text.Length > maxTextChars)
        {
            throw new UserInputException($"Text input is limited to {maxTextChars:N0} characters in the browser.");
        }
    }
}
