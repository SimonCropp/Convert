sealed class MarkdownToDocx : Conversion<NoOptions>
{
    public override string Id => "md-docx-officeimo";
    public override FileType Source => FileTypes.Markdown;
    public override FileType Target => FileTypes.Docx;
    public override Tool Tool => OfficeImoPack.Tool;
    public override string Summary => "Native Word headings, lists, tables and code blocks; Mermaid diagrams drawn by Naiad.";
    public override string Expect => "Mermaid diagrams are pictures with labels in the bundled font; one that does not parse stays code.";
    public override IReadOnlyList<string> Assemblies => OfficeImoPack.MarkdownWordDiagrams;

    // Naiad and Skia, about 1.9 MB, only for a file with a diagram for them to draw.
    public override IReadOnlyList<string> AssembliesFor(Document source) =>
        HasMermaid(source.Text) ? Assemblies : OfficeImoPack.MarkdownWord;

    protected override async Task<ConversionOutput> RunAsync(Document source, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var markdown = source.Text;
        var (bytes, notes) = await MarkdownRunner.ToDocxAsync(markdown, HasMermaid(markdown), progress, cancel);
        return new([new(FileTypes.Docx.FileNameFor(source.FileName), bytes, FileTypes.Docx)], notes);
    }

    /// <summary>
    /// Whether any line has a fence marker and "mermaid": every Mermaid fence, in a list or a quote too,
    /// and the odd line that only mentions one. When it says no, Naiad is not loaded and the run does not
    /// draw, so a miss would leave a diagram as code rather than fail.
    /// </summary>
    internal static bool HasMermaid(string markdown)
    {
        foreach (var line in markdown.AsSpan().EnumerateLines())
        {
            if ((line.Contains("```", StringComparison.Ordinal) || line.Contains("~~~", StringComparison.Ordinal)) &&
                line.Contains("mermaid", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
