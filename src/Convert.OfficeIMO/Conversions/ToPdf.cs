/// <summary>docx/xlsx/pptx/pub/html/md/txt to PDF.</summary>
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
