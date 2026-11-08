namespace Convert;

/// <summary>CSV or TSV to a one-sheet workbook.</summary>
sealed class DelimitedToXlsx(FileType source) : Conversion<NoOptions>
{
    public override string Id => $"{source.Id}-xlsx-officeimo";
    public override FileType Source => source;
    public override FileType Target => FileTypes.Xlsx;
    public override Tool Tool => OfficeImoPack.Tool;
    public override string Summary => "One worksheet named after the file, with the header row in bold.";
    public override string Expect => "Numbers and true/false become typed cells; values with a leading zero (codes, ZIPs) stay text.";
    public override IReadOnlyList<string> Assemblies => OfficeImoPack.ExcelData;

    protected override Task<ConversionOutput> RunAsync(Document document, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var bytes = SheetRunner.FromDelimited(document.Text, Delimited.For(source), document.FileName);
        return Task.FromResult(
            new ConversionOutput(
                new(
                    FileTypes.Xlsx.FileNameFor(document.FileName),
                    bytes,
                    FileTypes.Xlsx)));
    }
}
