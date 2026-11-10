/// <summary>An image drawn on a canvas and re-encoded: svg/png/jpeg/webp to png/jpeg/webp.</summary>
sealed class CanvasConversion(FileType source, FileType target, CanvasBridge canvas) : Conversion<RasterOptions>
{
    public override string Id => $"{source.Id}-{target.Id}-browser";
    public override FileType Source => source;
    public override FileType Target => target;
    public override Tool Tool => BrowserPack.Tool;

    public override string Summary
    {
        get
        {
            if (source == FileTypes.Svg)
            {
                return "Drawn by the browser's SVG renderer, at any scale.";
            }

            return "Re-encoded by the browser's own image codecs.";
        }
    }

    public override string? Expect
    {
        get
        {
            if (source == FileTypes.Svg)
            {
                return "SVG that embeds HTML (<foreignObject>) or external images can't be drawn on a canvas.";
            }

            if (target == FileTypes.Jpeg)
            {
                return "JPEG has no transparency: transparent pixels take the background colour.";
            }

            return null;
        }
    }

    public override IReadOnlyList<string> Assemblies => [];
    public override Type OptionsComponent => typeof(RasterOptionsPanel);

    public override object CreateOptions() =>
        new RasterOptions
        {
            Lossy = target != FileTypes.Png,
            SupportsAlpha = target != FileTypes.Jpeg,
            TransparentBackground = target != FileTypes.Jpeg &&
                                    source != FileTypes.Svg
        };

    protected override async Task<ConversionOutput> RunAsync(Document document, RasterOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var bytes = await canvas.RasterizeAsync(document.Bytes, source.ContentType, target.ContentType, options, cancel);
        return new(new(target.FileNameFor(document.FileName), bytes, target));
    }
}
