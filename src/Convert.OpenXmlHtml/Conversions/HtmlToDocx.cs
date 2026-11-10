sealed class HtmlToDocx : Conversion<HtmlDocxOptions>
{
    public override string Id => "html-docx-openxmlhtml";
    public override FileType Source => FileTypes.Html;
    public override FileType Target => FileTypes.Docx;
    public override Tool Tool => OpenXmlHtmlPack.Tool;
    public override string Summary => "Native Word headings, lists, tables and inline styles.";
    public override string Expect => "Images are not fetched (the browser can't read local files or most remote ones); scripts and forms are dropped.";
    public override IReadOnlyList<string> Assemblies => OpenXmlHtmlPack.Assemblies;
    public override Type OptionsComponent => typeof(HtmlDocxOptionsPanel);

    protected override Task<ConversionOutput> RunAsync(Document source, HtmlDocxOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var (bytes, notes) = OpenXmlHtmlRunner.ToDocx(source.Text, options);
        return Task.FromResult(new ConversionOutput([new(FileTypes.Docx.FileNameFor(source.FileName), bytes, FileTypes.Docx)], notes));
    }
}
