sealed class TextToDocx : Conversion<NoOptions>
{
    public override string Id => "txt-docx-officeimo";
    public override FileType Source => FileTypes.Text;
    public override FileType Target => FileTypes.Docx;
    public override Tool Tool => OfficeImoPack.Tool;
    public override string Summary => "Each line as a Word paragraph.";
    public override IReadOnlyList<string> Assemblies => OfficeImoPack.TextWord;

    protected override Task<ConversionOutput> RunAsync(Document source, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel) =>
        Task.FromResult(new ConversionOutput(new(FileTypes.Docx.FileNameFor(source.FileName), TextRunner.ToDocx(source.Text, cancel), FileTypes.Docx)));
}
