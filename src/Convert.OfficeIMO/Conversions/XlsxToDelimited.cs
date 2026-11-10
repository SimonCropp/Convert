/// <summary>Each worksheet to its own CSV or TSV file.</summary>
sealed class XlsxToDelimited(FileType target) : Conversion<NoOptions>
{
    public override string Id => $"xlsx-{target.Id}-officeimo";
    public override FileType Source => FileTypes.Xlsx;
    public override FileType Target => target;
    public override Tool Tool => OfficeImoPack.Tool;
    public override string Summary => "Cell values, one file per worksheet (several are downloaded as a zip).";
    public override string Expect => "Formulas give their last calculated value; formatting, merged cells and charts are not kept. Dates are written yyyy-mm-dd.";
    public override IReadOnlyList<string> Assemblies => OfficeImoPack.ExcelData;

    protected override Task<ConversionOutput> RunAsync(Document document, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var sheets = SheetRunner.ToDelimited(document.Bytes, Delimited.For(target), cancel);
        if (sheets.Count == 0)
        {
            throw new UserInputException("The workbook has no worksheets.");
        }

        var name = Path.GetFileNameWithoutExtension(document.FileName);
        IReadOnlyList<Document> parts = sheets.Count == 1
            ? [Document.FromText(target.FileNameFor(document.FileName), sheets[0].Text, target)]
            : [.. sheets.Select(_ => Document.FromText(target.FileNameFor($"{name} - {_.Sheet}"), _.Text, target))];
        return Task.FromResult(new ConversionOutput(parts, []));
    }
}
