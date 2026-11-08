namespace Convert;

/// <summary>SVG to an ICO holding each common size, each drawn sharp from the vector.</summary>
sealed class SvgIcoConversion : Conversion<NoOptions>
{
    public override string Id => "svg-ico-skia";
    public override FileType Source => FileTypes.Svg;
    public override FileType Target => FileTypes.Ico;
    public override Tool Tool => SkiaPack.Tool;
    public override string Summary => "16, 32, 48 and 256 pixels in one icon, each drawn from the vector.";
    public override string Expect => "A non-square drawing is centred on transparency. Scripts, animation and external images are not drawn.";
    public override IReadOnlyList<string> Assemblies => SkiaPack.SvgAssemblies;
    public override int Priority => SkiaPriority.Value;

    protected override async Task<ConversionOutput> RunAsync(Document document, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        progress.Report(new("Loading fonts"));
        var fonts = await SkiaFonts.EnsureAsync();
        progress.Report(new("Drawing", "ICO"));
        var bytes = SvgRunner.Ico(document.Text, fonts);
        return new(new Document(FileTypes.Ico.FileNameFor(document.FileName), bytes, FileTypes.Ico));
    }
}
