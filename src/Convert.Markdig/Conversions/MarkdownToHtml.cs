namespace Convert.Markdig;

sealed class MarkdownToHtml : Conversion<MarkdownHtmlOptions>
{
    public override string Id => "md-html-markdig";
    public override FileType Source => FileTypes.Markdown;
    public override FileType Target => FileTypes.Html;
    public override Tool Tool => MarkdigPack.Tool;
    public override string Summary => "CommonMark with GitHub-style extensions; fast and faithful.";
    public override string Expect => "Raw HTML in the Markdown passes through unchanged.";
    public override IReadOnlyList<string> Assemblies => MarkdigPack.Assemblies;
    public override Type OptionsComponent => typeof(MarkdownHtmlOptionsPanel);

    protected override Task<ConversionOutput> RunAsync(Document source, MarkdownHtmlOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var html = MarkdigRunner.ToHtml(source.Text, options);
        return Task.FromResult(new ConversionOutput(Document.FromText(FileTypes.Html.FileNameFor(source.FileName), html, FileTypes.Html)));
    }
}
