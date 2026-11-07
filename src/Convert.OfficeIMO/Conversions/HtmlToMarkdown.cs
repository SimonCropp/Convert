namespace Convert.OfficeIMO;

sealed class HtmlToMarkdown : Conversion<NoOptions>
{
    public override string Id => "html-md-officeimo";
    public override FileType Source => FileTypes.Html;
    public override FileType Target => FileTypes.Markdown;
    public override Tool Tool => OfficeImoPack.Tool;
    public override string Summary => "Headings, lists, tables, links and emphasis as portable Markdown.";
    public override string Expect => "Styling, scripts and layout are dropped.";
    public override IReadOnlyList<string> Assemblies => OfficeImoPack.HtmlMarkdown;

    protected override Task<ConversionOutput> RunAsync(Document source, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var (markdown, notes) = MarkdownRunner.FromHtml(source.Text);
        return Task.FromResult(new ConversionOutput([Document.FromText(FileTypes.Markdown.FileNameFor(source.FileName), markdown, FileTypes.Markdown)], notes));
    }
}