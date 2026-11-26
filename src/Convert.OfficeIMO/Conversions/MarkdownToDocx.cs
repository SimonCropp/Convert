sealed class MarkdownToDocx : Conversion<NoOptions>
{
    public override string Id => "md-docx-officeimo";
    public override FileType Source => FileTypes.Markdown;
    public override FileType Target => FileTypes.Docx;
    public override Tool Tool => OfficeImoPack.Tool;
    public override string Summary => "Native Word headings, lists, tables and code blocks; Mermaid diagrams drawn by Naiad.";
    public override string Expect => "Mermaid diagrams are pictures with labels in the bundled font; one that does not parse stays code.";
    public override IReadOnlyList<string> Assemblies => OfficeImoPack.MarkdownWordDiagrams;

    public override IReadOnlyList<string> AssembliesFor(Document source) =>
        MermaidFences.Any(source.Text) ? Assemblies : OfficeImoPack.MarkdownWord;

    protected override async Task<ConversionOutput> RunAsync(Document source, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var markdown = source.Text;
        var (bytes, notes) = await MarkdownRunner.ToDocxAsync(markdown, MermaidFences.Any(markdown), progress, cancel);
        return new([new(FileTypes.Docx.FileNameFor(source.FileName), bytes, FileTypes.Docx)], notes);
    }
}
