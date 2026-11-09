namespace Convert;

/// <summary>An SVG or raster image to the set of icons a website links, zipped with the HTML to paste.</summary>
sealed class FaviconConversion(FileType source) : Conversion<NoOptions>
{
    bool IsSvg => source == FileTypes.Svg;

    public override string Id => $"{source.Id}-favicons-skia";
    public override FileType Source => source;
    public override FileType Target => FileTypes.Favicons;
    public override Tool Tool => SkiaPack.Tool;

    public override string Summary =>
        IsSvg
            ? "favicon.ico, icon.svg, the Apple touch and manifest PNGs, site.webmanifest, and the <link> tags."
            : "favicon.ico, the Apple touch and manifest PNGs, site.webmanifest, and the <link> tags.";

    public override string Expect =>
        IsSvg
            ? "A non-square drawing is centred on transparency; the Apple icon is on white, as iOS needs."
            : "Start from at least 512×512 for sharp large icons. A non-square image is centred on transparency; the Apple icon is on white.";

    public override IReadOnlyList<string> Assemblies => IsSvg ? SkiaPack.SvgAssemblies : SkiaPack.RasterAssemblies;
    public override int Priority => SkiaPriority.Value;

    protected override async Task<ConversionOutput> RunAsync(Document document, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var name = Path.GetFileNameWithoutExtension(document.FileName);
        byte[] bytes;
        if (IsSvg)
        {
            progress.Report(new("Loading fonts"));
            var fonts = await SkiaFonts.EnsureAsync();
            progress.Report(new("Drawing", "icons"));
            bytes = SvgRunner.Favicons(document.Text, fonts, name);
        }
        else
        {
            progress.Report(new("Drawing", "icons"));
            bytes = SkiaRunner.Favicons(document.Bytes, name);
        }

        return new(new($"{name}-favicons.zip", bytes, FileTypes.Favicons));
    }
}
