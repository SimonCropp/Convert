/// <summary>
/// Raster images and SVG through Skia (SkiaSharp, and Svg.Skia for SVG): every format Skia decodes to
/// PNG, JPEG and WebP (the formats it encodes) and to PDF, SVG to all four and to ICO, and any of them
/// to a website's favicon set.
/// </summary>
public sealed class SkiaPack : IPack
{
    public static Tool Tool { get; } = new(
        "skia",
        "Skia",
        "Google's 2D graphics library (SkiaSharp, and Svg.Skia for SVG): raster codecs, SVG rendering and PDF.",
        "https://github.com/mono/SkiaSharp");

    public static IReadOnlyList<string> RasterAssemblies { get; } = ["SkiaSharp.wasm"];

    public static IReadOnlyList<string> SvgAssemblies { get; } =
    [
        .. RasterAssemblies,
        "Svg.Skia.wasm",
        "Svg.Model.wasm",
        "Svg.Custom.wasm",
        "Svg.SceneGraph.wasm",
        "Svg.Animation.wasm",
        "ShimSkiaSharp.wasm",
        "ExCSS.wasm",
        "HarfBuzzSharp.wasm"
    ];

    /// <summary>What Skia decodes.</summary>
    internal static IReadOnlyList<FileType> Decodes { get; } =
    [
        FileTypes.Png,
        FileTypes.Jpeg,
        FileTypes.WebP,
        FileTypes.Gif,
        FileTypes.Bmp,
        FileTypes.Ico,
        FileTypes.Wbmp
    ];

    /// <summary>What Skia encodes.</summary>
    internal static IReadOnlyList<FileType> Encodes { get; } = [FileTypes.Png, FileTypes.Jpeg, FileTypes.WebP];

    public void Register(ConversionRegistry registry)
    {
        registry.Add(FileTypes.Svg);
        registry.Add(FileTypes.Pdf);
        foreach (var format in Decodes)
        {
            registry.Add(format);
        }

        registry.Add(new Preview(FileTypes.Wbmp, RasterAssemblies, typeof(SkiaPreview)));

        foreach (var source in Decodes)
        {
            foreach (var target in Encodes.Where(_ => _ != source))
            {
                registry.Add(new SkiaRasterConversion(source, target));
            }

            registry.Add(new SkiaPdfConversion(source));
        }

        foreach (var target in Encodes)
        {
            registry.Add(new SvgRasterConversion(target));
        }

        registry.Add(new SvgPdfConversion());
        registry.Add(new SvgIcoConversion());

        registry.Add(new FaviconConversion(FileTypes.Svg));
        foreach (var source in Decodes)
        {
            registry.Add(new FaviconConversion(source));
        }

        registry.Add(new Preview(FileTypes.Favicons, [], typeof(ZipPreview)));

        // The source of ImageSharpPack's image samples (made from it by ImageSampleTests).
        registry.Add(new Sample(FileTypes.Svg.DisplayName, FileTypes.Svg.Icon, "_content/Convert.Skia/samples/sunset.svg", "sunset.svg", "Image"));
    }

    public void Loaded(IServiceProvider services) =>
        SkiaFonts.Initialize(services.GetRequiredService<HttpClient>());
}
