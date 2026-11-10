/// <summary>SVG drawn by Svg.Skia and encoded by Skia.</summary>
sealed class SvgRasterConversion(FileType target) : Conversion<RasterOptions>
{
    public override string Id => $"svg-{target.Id}-skia";
    public override FileType Source => FileTypes.Svg;
    public override FileType Target => target;
    public override Tool Tool => SkiaPack.Tool;
    public override string Summary => "Drawn by Svg.Skia in .NET, at any scale; text uses the bundled font.";
    public override string Expect => "Scripts, animation and external images are not drawn.";
    public override IReadOnlyList<string> Assemblies => SkiaPack.SvgAssemblies;
    public override Type OptionsComponent => typeof(RasterOptionsPanel);
    public override int Priority => SkiaPriority.Value;

    public override object CreateOptions() => RasterOptions.For(target);

    protected override async Task<ConversionOutput> RunAsync(Document document, RasterOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        progress.Report(new("Loading fonts"));
        var fonts = await SkiaFonts.EnsureAsync();
        progress.Report(new("Drawing", target.ShortName));
        var bytes = SvgRunner.Raster(document.Text, target, options, fonts);
        return new(new(target.FileNameFor(document.FileName), bytes, target));
    }
}
