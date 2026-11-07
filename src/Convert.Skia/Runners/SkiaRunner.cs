namespace Convert;

/// <summary>Raster routes: the only SkiaSharp code they reach, so they load SkiaSharp alone.</summary>
static class SkiaRunner
{
    // CSS pixels to PDF points.
    const float pointsPerPixel = 72f / 96f;

    public static byte[] Convert(byte[] bytes, FileType target, RasterOptions options)
    {
        using var image = Decode(bytes);
        var scale = options.Scale > 0 ? (float) options.Scale : 1f;
        var width = Math.Max(1, (int) MathF.Round(image.Width * scale));
        var height = Math.Max(1, (int) MathF.Round(image.Height * scale));
        using var surface = Surface(width, height);
        var canvas = surface.Canvas;
        canvas.Clear(Background(options));
        canvas.DrawImage(image, new SKRect(0, 0, width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        using var snapshot = surface.Snapshot();
        return Encode(snapshot, target, options);
    }

    /// <summary>The image on one PDF page of its own size.</summary>
    public static byte[] Pdf(byte[] bytes)
    {
        using var image = Decode(bytes);
        return Page(image.Width * pointsPerPixel, image.Height * pointsPerPixel, _ =>
        {
            _.Scale(pointsPerPixel);
            _.DrawImage(image, 0, 0, new SKSamplingOptions(SKCubicResampler.Mitchell));
        });
    }

    /// <summary>The image as a PNG, for formats a browser cannot show.</summary>
    public static byte[] Preview(byte[] bytes)
    {
        using var image = Decode(bytes);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    internal static SKSurface Surface(int width, int height) =>
        SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul)) ??
        throw new UserInputException($"A {width}×{height} image is too large to draw.");

    internal static SKColor Background(RasterOptions options) =>
        options.Flatten is { } hex && SKColor.TryParse(hex, out var color) ? color : SKColors.Transparent;

    internal static byte[] Encode(SKImage image, FileType target, RasterOptions options)
    {
        var format = target.Id switch
        {
            "png" => SKEncodedImageFormat.Png,
            "jpeg" => SKEncodedImageFormat.Jpeg,
            "webp" => SKEncodedImageFormat.Webp,
            _ => throw new NotSupportedException($"Skia does not encode {target.DisplayName}.")
        };
        using var data = image.Encode(format, Math.Clamp(options.Quality, 1, 100)) ??
                         throw new InvalidOperationException($"Skia could not encode {target.DisplayName}.");
        return data.ToArray();
    }

    /// <summary>A one-page PDF of <paramref name="width"/>×<paramref name="height"/> points.</summary>
    internal static byte[] Page(float width, float height, Action<SKCanvas> draw)
    {
        using var stream = new SKDynamicMemoryWStream();
        using (var document = SKDocument.CreatePdf(stream) ??
                              throw new InvalidOperationException("This Skia build has no PDF backend."))
        {
            var canvas = document.BeginPage(width, height);
            draw(canvas);
            document.EndPage();
            document.Close();
        }

        using var data = stream.DetachAsData();
        return data.ToArray();
    }

    static SKImage Decode(byte[] bytes) =>
        SKImage.FromEncodedData(bytes) ??
        throw new UserInputException("The image could not be read: Skia does not recognise its format or the file is damaged.");
}
