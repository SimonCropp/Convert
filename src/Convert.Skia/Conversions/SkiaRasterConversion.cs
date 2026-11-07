namespace Convert;

/// <summary>A raster image decoded and re-encoded by Skia.</summary>
sealed class SkiaRasterConversion(FileType source, FileType target) : Conversion<RasterOptions>
{
    public override string Id => $"{source.Id}-{target.Id}-skia";
    public override FileType Source => source;
    public override FileType Target => target;
    public override Tool Tool => SkiaPack.Tool;
    public override string Summary => "Decoded and re-encoded by Skia's native codecs.";

    public override string? Expect =>
        source == FileTypes.Gif
            ? "An animated GIF keeps only its first frame."
            : RasterOptions.HasAlpha(target)
                ? null
                : $"{target.DisplayName} has no transparency: transparent pixels take the background colour.";

    public override IReadOnlyList<string> Assemblies => SkiaPack.RasterAssemblies;
    public override Type OptionsComponent => typeof(RasterOptionsPanel);
    public override int Priority => SkiaPriority.Value;

    public override object CreateOptions() => RasterOptions.For(target);

    protected override Task<ConversionOutput> RunAsync(Document document, RasterOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var bytes = SkiaRunner.Convert(document.Bytes, target, options);
        return Task.FromResult(
            new ConversionOutput(
                new(
                    target.FileNameFor(document.FileName),
                    bytes,
                    target)));
    }
}