namespace Convert;

/// <summary>
/// Raster images through ImageSharp, a managed codec set: every format it reads to every format it
/// writes, and a preview for the formats a browser cannot show.
/// </summary>
public sealed class ImageSharpPack : IPack
{
    public static Tool Tool { get; } = new(
        "imagesharp",
        "ImageSharp",
        "Managed image codecs: reads and writes PNG, JPEG, WebP, GIF, TIFF, BMP, ICO, TGA, QOI, EXR and more.",
        "https://github.com/SixLabors/ImageSharp");

    internal static IReadOnlyList<string> Assemblies { get; } = ["SixLabors.ImageSharp.wasm", "System.IO.Hashing.wasm"];

    /// <summary>Every format ImageSharp both reads and writes.</summary>
    internal static IReadOnlyList<FileType> Formats { get; } =
    [
        FileTypes.Png,
        FileTypes.Jpeg,
        FileTypes.WebP,
        FileTypes.Gif,
        FileTypes.Bmp,
        FileTypes.Tiff,
        FileTypes.Ico,
        FileTypes.Cur,
        FileTypes.Ani,
        FileTypes.Tga,
        FileTypes.Qoi,
        FileTypes.Netpbm,
        FileTypes.Exr
    ];

    /// <summary>Formats no browser's <c>&lt;img&gt;</c> shows everywhere: previewed as a PNG ImageSharp draws.</summary>
    static readonly FileType[] decodedPreviews =
    [
        FileTypes.Tiff,
        FileTypes.Cur,
        FileTypes.Ani,
        FileTypes.Tga,
        FileTypes.Qoi,
        FileTypes.Netpbm,
        FileTypes.Exr
    ];

    public void Register(ConversionRegistry registry)
    {
        foreach (var format in Formats)
        {
            registry.Add(format);
        }

        foreach (var format in decodedPreviews)
        {
            registry.Add(new Preview(format, Assemblies, typeof(ImageSharpPreview)));
        }

        foreach (var source in Formats)
        {
            foreach (var target in Formats.Where(_ => _ != source))
            {
                registry.Add(new ImageSharpConversion(source, target));
            }
        }

        // SkiaPack's sunset.svg in every format (made by ImageSampleTests).
        foreach (var format in Formats)
        {
            var file = format.FileNameFor("sunset");
            registry.Add(new Sample(format.DisplayName, format.Icon, $"_content/Convert.ImageSharp/samples/{file}", file, "Image"));
        }
    }
}
