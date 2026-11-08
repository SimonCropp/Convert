namespace Convert;

/// <summary>PDF to docx/xlsx/pptx/html/png/txt/md.</summary>
sealed class FromPdf(FileType target, IReadOnlyList<string> assemblies, string summary) : Conversion
{
    public override string Id => $"pdf-{target.Id}-officeimo";
    public override FileType Source => FileTypes.Pdf;
    public override FileType Target => target;
    public override Tool Tool => OfficeImoPack.Tool;
    public override string Summary => summary;

    public override string Expect =>
        target.Id switch
        {
            "xlsx" => "Only tables are imported; other page content is left out.",
            "png" => "Several pages download as a zip of page_0001.png, page_0002.png, ….",
            "txt" => "The text layer only: a scanned PDF has none. Reading order follows the page layout.",
            "md" => "Headings, lists and tables are inferred from the layout; images are left out.",
            _ => "A reconstruction: layout that PDF only implies (columns, flow) is approximated."
        };

    public override IReadOnlyList<string> Assemblies => assemblies;

    public override Type? OptionsComponent =>
        target.Id switch
        {
            "pptx" => typeof(PdfSlidesOptionsPanel),
            "png" => typeof(PdfPngOptionsPanel),
            "html" => typeof(PrettyPrintOptionsPanel),
            _ => null
        };

    public override object CreateOptions() =>
        target.Id switch
        {
            "pptx" => new PdfSlidesOptions(),
            "png" => new PdfPngOptions(),
            "html" => new PrettyPrintOptions(),
            _ => new NoOptions()
        };

    public override async Task<ConversionOutput> RunAsync(Document document, object options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        progress.Report(new("Loading fonts"));
        var fonts = await OfficeImoFonts.EnsureAsync();
        progress.Report(new("Converting", $"PDF to {target.ShortName}"));
        var output = PdfImportRunner.Convert(document, target, options, fonts, cancel);
        return options is PrettyPrintOptions pretty ? PrettyPrint.Apply(output, pretty.PrettyPrint) : output;
    }
}