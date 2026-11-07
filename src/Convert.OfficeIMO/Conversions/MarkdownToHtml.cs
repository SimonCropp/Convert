namespace Convert.OfficeIMO;

sealed class MarkdownToHtml : Conversion<NoOptions>
{
    public override string Id => "md-html-officeimo";
    public override FileType Source => FileTypes.Markdown;
    public override FileType Target => FileTypes.Html;
    public override Tool Tool => OfficeImoPack.Tool;
    public override string Summary => "Strict, portable HTML fragment; raw HTML in the Markdown is sanitised.";
    public override IReadOnlyList<string> Assemblies => OfficeImoPack.MarkdownHtml;

    // Markdig serves Markdown to HTML first.
    public override int Priority => 10;

    protected override Task<ConversionOutput> RunAsync(Document source, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel) =>
        Task.FromResult(
            new ConversionOutput(
                Document.FromText(
                    FileTypes.Html.FileNameFor(source.FileName),
                    MarkdownRunner.ToHtml(source.Text),
                    FileTypes.Html)));
}