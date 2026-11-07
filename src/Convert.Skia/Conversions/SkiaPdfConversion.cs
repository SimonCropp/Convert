namespace Convert;

/// <summary>A raster image on a PDF page of its own size, through Skia's PDF backend.</summary>
sealed class SkiaPdfConversion(FileType source) : Conversion<NoOptions>
{
    public override string Id => $"{source.Id}-pdf-skia";
    public override FileType Source => source;
    public override FileType Target => FileTypes.Pdf;
    public override Tool Tool => SkiaPack.Tool;
    public override string Summary => "One page the size of the image (at 96 DPI), through Skia's PDF backend.";
    public override string? Expect => source == FileTypes.Gif ? "An animated GIF keeps only its first frame." : null;
    public override IReadOnlyList<string> Assemblies => SkiaPack.RasterAssemblies;
    public override int Priority => SkiaPriority.Value;

    protected override Task<ConversionOutput> RunAsync(Document document, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var bytes = SkiaRunner.Pdf(document.Bytes);
        return Task.FromResult(
            new ConversionOutput(
                new(
                    FileTypes.Pdf.FileNameFor(document.FileName),
                    bytes,
                    FileTypes.Pdf)));
    }
}