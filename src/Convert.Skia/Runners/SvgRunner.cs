using Svg.Skia;
using Svg.Skia.TypefaceProviders;

namespace Convert;

/// <summary>
/// SVG routes, through Svg.Skia (and Mermaid to PDF, in the Naiad pack). Kept apart from
/// <see cref="SkiaRunner"/> so raster routes never load Svg.*.
/// </summary>
public static class SvgRunner
{
    // CSS pixels to PDF points.
    const float pointsPerPixel = 72f / 96f;

    public static byte[] Raster(string text, FileType target, RasterOptions options, SkiaFonts.Faces fonts)
    {
        using var typefaces = new AptosTypefaces(fonts);
        using var svg = Load(text, typefaces);
        var picture = svg.Picture!;
        var bounds = picture.CullRect;
        var scale = options.Scale > 0 ? (float) options.Scale : 1f;
        var width = Math.Max(1, (int) MathF.Ceiling(bounds.Width * scale));
        var height = Math.Max(1, (int) MathF.Ceiling(bounds.Height * scale));
        using var surface = SkiaRunner.Surface(width, height);
        var canvas = surface.Canvas;
        canvas.Clear(SkiaRunner.Background(options));
        canvas.Scale(scale);
        canvas.Translate(-bounds.Left, -bounds.Top);
        canvas.DrawPicture(picture);
        using var snapshot = surface.Snapshot();
        return SkiaRunner.Encode(snapshot, target, options);
    }

    public static byte[] Pdf(string text, SkiaFonts.Faces fonts)
    {
        using var typefaces = new AptosTypefaces(fonts);
        using var svg = Load(text, typefaces);
        var picture = svg.Picture!;
        var bounds = picture.CullRect;
        return SkiaRunner.Page(bounds.Width * pointsPerPixel, bounds.Height * pointsPerPixel, _ =>
        {
            _.Scale(pointsPerPixel);
            _.Translate(-bounds.Left, -bounds.Top);
            _.DrawPicture(picture);
        });
    }

    internal static byte[] Ico(string text, SkiaFonts.Faces fonts)
    {
        using var typefaces = new AptosTypefaces(fonts);
        using var svg = Load(text, typefaces);
        return IconPackager.Ico([.. IconPackager.IcoSizes.Select(_ => (_, Square(svg.Picture!, _, false)))]);
    }

    /// <summary>The favicon set, with the SVG itself as icon.svg for browsers that take one.</summary>
    internal static byte[] Favicons(string text, SkiaFonts.Faces fonts, string name)
    {
        using var typefaces = new AptosTypefaces(fonts);
        using var svg = Load(text, typefaces);
        var picture = svg.Picture!;
        return IconPackager.Favicons((size, opaque) => Square(picture, size, opaque), text, name);
    }

    static byte[] Square(SKPicture picture, int size, bool opaque)
    {
        var bounds = picture.CullRect;
        return SkiaRunner.Square(size, opaque, bounds.Width, bounds.Height, (canvas, scale) =>
        {
            canvas.Scale(scale);
            canvas.Translate(-bounds.Left, -bounds.Top);
            canvas.DrawPicture(picture);
        });
    }

    static SKSvg Load(string text, AptosTypefaces typefaces)
    {
        var svg = new SKSvg();
        svg.Settings.TypefaceProviders = [typefaces];
        try
        {
            if (svg.FromSvg(text) is
                {
                    CullRect:
                    {
                        Width: > 0,
                        Height: > 0
                    }
                })
            {
                return svg;
            }
        }
        catch (Exception exception) when (exception is XmlException or FormatException or InvalidOperationException)
        {
            svg.Dispose();
            throw new UserInputException($"The SVG could not be read: {exception.Message}", exception);
        }

        svg.Dispose();
        throw new UserInputException("The SVG has nothing to draw, or no size.");
    }

    /// <summary>Every family, Aptos: the weight and slant asked for pick the face.</summary>
    sealed class AptosTypefaces(SkiaFonts.Faces fonts) : ITypefaceProvider, IDisposable
    {
        SKTypeface? regular;
        SKTypeface? bold;
        SKTypeface? italic;
        SKTypeface? boldItalic;

        public SKTypeface FromFamilyName(string fontFamily, SKFontStyleWeight fontWeight, SKFontStyleWidth fontWidth, SKFontStyleSlant fontStyle)
        {
            var heavy = fontWeight >= SKFontStyleWeight.SemiBold;
            var slanted = fontStyle != SKFontStyleSlant.Upright;
            return (heavy, slanted) switch
            {
                (true, true) => boldItalic ??= Create(fonts.BoldItalic),
                (true, false) => bold ??= Create(fonts.Bold),
                (false, true) => italic ??= Create(fonts.Italic),
                _ => regular ??= Create(fonts.Regular)
            };
        }

        static SKTypeface Create(byte[] bytes)
        {
            using var data = SKData.CreateCopy(bytes);
            return SKTypeface.FromData(data) ?? SKTypeface.Default;
        }

        public void Dispose()
        {
            regular?.Dispose();
            bold?.Dispose();
            italic?.Dispose();
            boldItalic?.Dispose();
        }
    }
}
