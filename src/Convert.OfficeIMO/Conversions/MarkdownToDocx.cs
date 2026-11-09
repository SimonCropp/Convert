namespace Convert;

sealed class MarkdownToDocx : Conversion<NoOptions>
{
    public override string Id => "md-docx-officeimo";
    public override FileType Source => FileTypes.Markdown;
    public override FileType Target => FileTypes.Docx;
    public override Tool Tool => OfficeImoPack.Tool;
    public override string Summary => "Native Word headings, lists, tables and code blocks.";
    public override IReadOnlyList<string> Assemblies => OfficeImoPack.MarkdownWord;

    protected override Task<ConversionOutput> RunAsync(Document source, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel) =>
        Task.FromResult(new ConversionOutput(new(FileTypes.Docx.FileNameFor(source.FileName), MarkdownRunner.ToDocx(source.Text), FileTypes.Docx)));
}
