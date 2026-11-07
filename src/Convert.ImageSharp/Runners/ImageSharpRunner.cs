using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Ani;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Cur;
using SixLabors.ImageSharp.Formats.Exr;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Ico;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Qoi;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Convert.ImageSharp;

/// <summary>The only place ImageSharp's namespaces are used. Every route loads only SixLabors.ImageSharp.</summary>
static class ImageSharpRunner
{
    const int maxIconSize = 256;

    public static byte[] Convert(byte[] bytes, FileType target, RasterOptions options, Cancel cancel)
    {
        using var image = Load(bytes);
        cancel.ThrowIfCancellationRequested();
        Prepare(image, target, options);
        cancel.ThrowIfCancellationRequested();
        using var output = new MemoryStream();
        image.Save(output, Encoder(target, options));
        return output.ToArray();
    }

    /// <summary>The image as a PNG, for formats a browser cannot show.</summary>
    public static byte[] Preview(byte[] bytes)
    {
        using var image = Load(bytes);
        using var output = new MemoryStream();
        image.Save(output, new PngEncoder());
        return output.ToArray();
    }

    static Image Load(byte[] bytes)
    {
        try
        {
            return Image.Load(bytes);
        }
        catch (Exception exception) when (exception is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            throw new UserInputException($"The image could not be read: {exception.Message}", exception);
        }
    }

    // Scale, fit an icon into 256×256, and flatten onto the background where transparency is not kept.
    static void Prepare(Image image, FileType target, RasterOptions options)
    {
        var scale = options.Scale > 0 ? options.Scale : 1;
        if (ImageSharpConversion.IsIcon(target))
        {
            scale = Math.Min(scale, (double) maxIconSize / Math.Max(image.Width, image.Height));
        }

        var width = Math.Max(1, (int) Math.Round(image.Width * scale));
        var height = Math.Max(1, (int) Math.Round(image.Height * scale));
        var resize = width != image.Width || height != image.Height;
        var background = options.Flatten is { } hex ? Color.ParseHex(hex) : (Color?) null;
        if (!resize && background is null)
        {
            return;
        }

        image.Mutate(_ =>
        {
            if (resize)
            {
                _.Resize(width, height);
            }

            if (background is { } color)
            {
                _.BackgroundColor(color);
            }
        });
    }

    static IImageEncoder Encoder(FileType target, RasterOptions options) =>
        target.Id switch
        {
            "png" => new PngEncoder(),
            "jpeg" => new JpegEncoder { Quality = Quality(options) },
            "webp" => new WebpEncoder { FileFormat = WebpFileFormatType.Lossy, Quality = Quality(options) },
            "gif" => new GifEncoder(),
            "bmp" => new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Bit32, SupportTransparency = true },
            "tiff" => new TiffEncoder(),
            "ico" => new IcoEncoder(),
            "cur" => new CurEncoder(),
            "ani" => new AniEncoder(),
            "tga" => new TgaEncoder(),
            "qoi" => new QoiEncoder(),
            "pnm" => new PbmEncoder(),
            "exr" => new ExrEncoder(),
            _ => throw new NotSupportedException($"ImageSharp does not write {target.DisplayName} here.")
        };

    static int Quality(RasterOptions options) =>
        Math.Clamp(options.Quality, 1, 100);
}
