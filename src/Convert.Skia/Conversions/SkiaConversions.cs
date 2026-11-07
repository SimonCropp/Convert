namespace Convert.Skia;

// After the browser's canvas (no download) and ImageSharp (managed) wherever they serve the same pair.
static class SkiaPriority
{
    public const int Value = 20;
}

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
    public override Type OptionsComponent => typeof(Components.RasterOptionsPanel);
    public override int Priority => SkiaPriority.Value;

    public override object CreateOptions() => RasterOptions.For(target);

    protected override Task<ConversionOutput> RunAsync(Document document, RasterOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var bytes = SkiaRunner.Convert(document.Bytes, target, options);
        return Task.FromResult(new ConversionOutput(new Document(target.FileNameFor(document.FileName), bytes, target)));
    }
}

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
        return Task.FromResult(new ConversionOutput(new Document(FileTypes.Pdf.FileNameFor(document.FileName), bytes, FileTypes.Pdf)));
    }
}

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
    public override Type OptionsComponent => typeof(Components.RasterOptionsPanel);
    public override int Priority => SkiaPriority.Value;

    public override object CreateOptions() => RasterOptions.For(target);

    protected override async Task<ConversionOutput> RunAsync(Document document, RasterOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        progress.Report(new("Loading fonts"));
        var fonts = await SkiaFonts.EnsureAsync();
        progress.Report(new("Drawing", target.ShortName));
        var bytes = SvgRunner.Raster(document.Text, target, options, fonts);
        return new(new Document(target.FileNameFor(document.FileName), bytes, target));
    }
}

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
        return new(new Document(FileTypes.Pdf.FileNameFor(document.FileName), bytes, FileTypes.Pdf));
    }
}
