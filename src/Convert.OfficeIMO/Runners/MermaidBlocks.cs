using OfficeIMO;
using OfficeIMO.Markdown;

/// <summary>
/// Mermaid fences drawn by Naiad. OfficeIMO's Word writer has no diagram renderer and sets a Mermaid
/// fence as a code panel; each one becomes a PNG image block instead, sized to the diagram, so OfficeIMO
/// inserts and fits it like any other picture. Not SVG: OfficeIMO's own SVG reader rejects Naiad's
/// output, so a Word SVG picture would have no PNG fallback. A diagram Naiad cannot draw stays code, with
/// a note.
/// </summary>
static class MermaidBlocks
{
    // Twice the diagram's own size: sharp on screen and in print, and shrunk to fit wide diagrams.
    const double scale = 2;

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
}
