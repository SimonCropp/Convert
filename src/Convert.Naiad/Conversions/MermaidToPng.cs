sealed class MermaidToPng : Conversion<MermaidOptions>
{
    public override string Id => "mermaid-png-naiad";
    public override FileType Source => FileTypes.Mermaid;
    public override FileType Target => FileTypes.Png;
    public override Tool Tool => NaiadPack.Tool;
    public override string Summary => "Rasterised by Skia, at any scale.";
    public override string Expect => "Labels use the bundled font; HTML labels are drawn as plain text.";
    public override IReadOnlyList<string> Assemblies => NaiadPack.SkiaAssemblies;
    public override Type OptionsComponent => typeof(MermaidOptionsPanel);

    public override object CreateOptions() => new MermaidOptions { Png = true };

    protected override async Task<ConversionOutput> RunAsync(Document source, MermaidOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        progress.Report(new("Loading fonts"));
        var fonts = await SkiaFonts.EnsureAsync();
        progress.Report(new("Drawing", "PNG"));
        var svg = MermaidRunner.PlainSvg(source.Text, options);
        var raster = new RasterOptions
        {
            Scale = options.PngScale,
            Background = options.Background,
            SupportsAlpha = true,
            TransparentBackground = options.TransparentBackground
        };
        var png = SvgRunner.Raster(svg, FileTypes.Png, raster, fonts);
        return new(new(FileTypes.Png.FileNameFor(source.FileName), png, FileTypes.Png));
    }
}
