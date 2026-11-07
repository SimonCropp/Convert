namespace Convert;

// After the browser's canvas (no download) and ImageSharp (managed) wherever they serve the same pair.

/// <summary>SVG to vector PDF: Svg.Skia's drawing replayed into Skia's PDF backend.</summary>
sealed class SvgPdfConversion : Conversion<NoOptions>
{
    public override string Id => "svg-pdf-skia";
    public override FileType Source => FileTypes.Svg;
    public override FileType Target => FileTypes.Pdf;
    public override Tool Tool => SkiaPack.Tool;
    public override string Summary => "Vector PDF, one page the size of the drawing; text uses the bundled font.";
    public override string Expect => "Scripts, animation and external images are not drawn.";
    public override IReadOnlyList<string> Assemblies => SkiaPack.SvgAssemblies;
    public override int Priority => SkiaPriority.Value;

    protected override async Task<ConversionOutput> RunAsync(Document document, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        progress.Report(new("Loading fonts"));
        var fonts = await SkiaFonts.EnsureAsync();
        progress.Report(new("Drawing", "PDF"));
        var bytes = SvgRunner.Pdf(document.Text, fonts);
        return new(
            new(
                FileTypes.Pdf.FileNameFor(document.FileName),
                bytes,
                FileTypes.Pdf));
    }
}
