using System.Globalization;
using OfficeIMO;
using OfficeIMO.Markdown;
using OfficeIMO.MarkdownRenderer;

/// <summary>
/// Mermaid fences drawn by Naiad. OfficeIMO has no diagram renderer and sets a Mermaid fence as code;
/// each one becomes a PNG picture instead, sized to the diagram, so OfficeIMO inserts and fits it like any
/// other picture. Not SVG: OfficeIMO's own SVG reader rejects Naiad's output, so a Word SVG picture would
/// have no PNG fallback, and the PDF layout could not draw it. A diagram Naiad cannot draw stays code,
/// with a note.
/// </summary>
static class MermaidBlocks
{
    // Twice the diagram's own size: sharp on screen and in print, and shrunk to fit wide diagrams.
    const double scale = 2;

    /// <summary>
    /// For OfficeIMO's Markdown to HTML on the way to PDF: each Mermaid fence as an image, in place of
    /// the code block the renderer would emit.
    /// </summary>
    public static MarkdownFencedCodeBlockRenderer HtmlRenderer(SkiaFonts.Faces fonts, List<string> notes) =>
        new("Naiad Mermaid", ["mermaid"], new HtmlImages(fonts, notes).Render);

    public static async Task DrawAsync(MarkdownDocument document, List<string> notes, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var diagrams = document.DescendantsAndSelf().Where(IsMermaid).ToList();
        if (diagrams.Count == 0)
        {
            return;
        }

        progress.Report(new("Loading fonts"));
        var fonts = await SkiaFonts.EnsureAsync();
        var images = new Dictionary<IMarkdownBlock, IMarkdownBlock>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < diagrams.Count; i++)
        {
            cancel.ThrowIfCancellationRequested();
            progress.Report(new("Drawing", $"Mermaid diagram {i + 1} of {diagrams.Count}"));
            var diagram = diagrams[i];
            var (source, caption) = diagram switch
            {
                CodeBlock code => (code.Content, code.Caption),
                SemanticFencedBlock semantic => (semantic.Content, semantic.Caption),
                _ => throw new InvalidOperationException()
            };
            try
            {
                var (png, width, height) = MermaidImage.Png(source, fonts, scale);
                images[diagram] = new ImageBlock(
                    $"data:image/png;base64,{System.Convert.ToBase64String(png)}",
                    "Mermaid diagram",
                    width: OfficeLength.Pixels(width),
                    height: OfficeLength.Pixels(height))
                {
                    Caption = caption
                };
            }
            catch (UserInputException exception)
            {
                notes.Add($"Mermaid diagram {i + 1} is kept as code: {exception.Message}");
            }
        }

        if (images.Count > 0)
        {
            document.Rewrite(new Replace(images));
        }
    }

    // A fence parses as a code block; a reader configured for semantic fences makes it one of those.
    static bool IsMermaid(IMarkdownBlock block) =>
        block switch
        {
            CodeBlock code => IsMermaid(code.Language),
            SemanticFencedBlock semantic => IsMermaid(semantic.SemanticKind) || IsMermaid(semantic.Language),
            _ => false
        };

    static bool IsMermaid(string? language) =>
        string.Equals(language, "mermaid", StringComparison.OrdinalIgnoreCase);

    sealed class Replace(Dictionary<IMarkdownBlock, IMarkdownBlock> images) : MarkdownRewriter
    {
        protected override IMarkdownBlock RewriteCurrentBlock(IMarkdownBlock block) =>
            images.GetValueOrDefault(block, block);
    }

    // The renderer calls back per fence, in document order; null keeps its code block. It asks again for a
    // block it got null for, so each diagram is drawn, and noted, once. The image is capped at the column
    // width: OfficeIMO's PDF layout would otherwise shrink the whole page to fit a wide diagram, text and all.
    sealed class HtmlImages(SkiaFonts.Faces fonts, List<string> notes)
    {
        Dictionary<string, string?> drawn = new(StringComparer.Ordinal);

        public string? Render(MarkdownFencedCodeBlockMatch match, MarkdownRendererOptions options)
        {
            var mermaid = match.RawContent;
            if (!drawn.TryGetValue(mermaid, out var html))
            {
                html = Draw(mermaid, drawn.Count + 1);
                drawn[mermaid] = html;
            }

            return html;
        }

        string? Draw(string mermaid, int number)
        {
            try
            {
                var (png, width, height) = MermaidImage.Png(mermaid, fonts, scale);
                return string.Create(
                    CultureInfo.InvariantCulture,
                    $"""<p><img src="data:image/png;base64,{System.Convert.ToBase64String(png)}" alt="Mermaid diagram" width="{width:0.##}" height="{height:0.##}" style="max-width: 100%; height: auto"></p>""");
            }
            catch (UserInputException exception)
            {
                notes.Add($"Mermaid diagram {number} is kept as code: {exception.Message}");
                return null;
            }
        }
    }
}
