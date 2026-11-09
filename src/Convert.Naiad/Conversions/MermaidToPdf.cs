namespace Convert;

/// <summary>Naiad's SVG drawn into Skia's PDF backend: a vector PDF of the diagram.</summary>
sealed class MermaidToPdf : Conversion<MermaidOptions>
{
    public override string Id => "mermaid-pdf-naiad";
    public override FileType Source => FileTypes.Mermaid;
    public override FileType Target => FileTypes.Pdf;
    public override Tool Tool => NaiadPack.Tool;
    public override string Summary => "Vector PDF, one page the size of the diagram, drawn by Skia.";
    public override string Expect => "Labels use the bundled font; HTML labels are drawn as plain text.";
    public override IReadOnlyList<string> Assemblies => NaiadPack.SkiaAssemblies;
    public override Type OptionsComponent => typeof(MermaidOptionsPanel);

    protected override async Task<ConversionOutput> RunAsync(Document source, MermaidOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        progress.Report(new("Loading fonts"));
        var fonts = await SkiaFonts.EnsureAsync();
        progress.Report(new("Drawing", "PDF"));
        var svg = MermaidRunner.PlainSvg(source.Text, options);
        var bytes = SvgRunner.Pdf(svg, fonts);
        return new(new Document(FileTypes.Pdf.FileNameFor(source.FileName), bytes, FileTypes.Pdf));
    }
}
