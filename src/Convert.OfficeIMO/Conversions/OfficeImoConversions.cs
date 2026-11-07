namespace Convert.OfficeIMO;

/// <summary>docx/xlsx/pptx/pub/html to PDF.</summary>
sealed class ToPdf(FileType source, IReadOnlyList<string> assemblies, string summary) : Conversion<OfficeImoPdfOptions>
{
    public override string Id => $"{source.Id}-pdf-officeimo";
    public override FileType Source => source;
    public override FileType Target => FileTypes.Pdf;
    public override Tool Tool => OfficeImoPack.Tool;
    public override string Summary => summary;
    public override string Expect => "Fonts are replaced by the bundled Aptos; complex scripts (Arabic, CJK) are not shaped. Warnings are listed under the result.";
    public override IReadOnlyList<string> Assemblies => assemblies;
    public override Type OptionsComponent => typeof(OfficeImoPdfOptionsPanel);

    // Morph serves Office to PDF first; OfficeIMO is the alternative.
    public override int Priority => 10;

    public override object CreateOptions() => new OfficeImoPdfOptions { Excel = source == FileTypes.Xlsx };

    protected override async Task<ConversionOutput> RunAsync(Document document, OfficeImoPdfOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        progress.Report(new("Loading fonts"));
        var fonts = await OfficeImoFonts.EnsureAsync();
        progress.Report(new("Converting", $"{source.ShortName} to PDF"));
        var (bytes, notes) = WordRunner.ToPdf(document, options, fonts, cancel);
        return new([new(FileTypes.Pdf.FileNameFor(document.FileName), bytes, FileTypes.Pdf)], notes);
    }
}

/// <summary>PDF to docx/xlsx/pptx/html/png.</summary>
sealed class FromPdf(FileType target, IReadOnlyList<string> assemblies, string summary) : Conversion
{
    public override string Id => $"pdf-{target.Id}-officeimo";
    public override FileType Source => FileTypes.Pdf;
    public override FileType Target => target;
    public override Tool Tool => OfficeImoPack.Tool;
    public override string Summary => summary;

    public override string? Expect =>
        target.Id switch
        {
            "xlsx" => "Only tables are imported; other page content is left out.",
            "png" => "Several pages download as a zip of page_0001.png, page_0002.png, ….",
            _ => "A reconstruction: layout that PDF only implies (columns, flow) is approximated."
        };

    public override IReadOnlyList<string> Assemblies => assemblies;

    public override Type? OptionsComponent =>
        target.Id switch
        {
            "pptx" => typeof(PdfSlidesOptionsPanel),
            "png" => typeof(PdfPngOptionsPanel),
            _ => null
        };

    public override object CreateOptions() =>
        target.Id switch
        {
            "pptx" => new PdfSlidesOptions(),
            "png" => new PdfPngOptions(),
            _ => new NoOptions()
        };

    public override async Task<ConversionOutput> RunAsync(Document document, object options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        progress.Report(new("Loading fonts"));
        var fonts = await OfficeImoFonts.EnsureAsync();
        progress.Report(new("Converting", $"PDF to {target.ShortName}"));
        return PdfImportRunner.Convert(document, target, options, fonts, cancel);
    }
}

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
        Task.FromResult(new ConversionOutput(Document.FromText(FileTypes.Html.FileNameFor(source.FileName), MarkdownRunner.ToHtml(source.Text), FileTypes.Html)));
}

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

sealed class MarkdownToDocx : Conversion<NoOptions>
{
    public override string Id => "md-docx-officeimo";
    public override FileType Source => FileTypes.Markdown;
    public override FileType Target => FileTypes.Docx;
    public override Tool Tool => OfficeImoPack.Tool;
    public override string Summary => "Native Word headings, lists, tables and code blocks.";
    public override IReadOnlyList<string> Assemblies => OfficeImoPack.MarkdownWord;

    protected override Task<ConversionOutput> RunAsync(Document source, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel) =>
        Task.FromResult(new ConversionOutput(new Document(FileTypes.Docx.FileNameFor(source.FileName), MarkdownRunner.ToDocx(source.Text), FileTypes.Docx)));
}
