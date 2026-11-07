namespace Convert.Browser;

/// <summary>
/// What the browser does on its own: PDF preview in its built-in viewer, and image conversions through
/// a canvas. Browsers expose no API to rasterise a PDF, so PDF to PNG is OfficeIMO's.
/// </summary>
public sealed class BrowserPack : IPack
{
    public static Tool Tool { get; } = new(
        "browser",
        "Your browser",
        "The browser's own image decoders and canvas, with no download.",
        "https://developer.mozilla.org/docs/Web/API/HTMLCanvasElement/toBlob");

    // Per pack instance, not static: the JavaScript runtime arrives in Loaded.
    CanvasBridge canvas = new();

    public void Register(ConversionRegistry registry)
    {
        registry.Add(FileTypes.Pdf);
        registry.Add(new Preview(FileTypes.Pdf, [], typeof(Previews.PdfPreview)));

        FileType[] rasters = [FileTypes.Png, FileTypes.Jpeg, FileTypes.WebP];
        foreach (var target in rasters)
        {
            registry.Add(new CanvasConversion(FileTypes.Svg, target, canvas));
        }

        foreach (var source in rasters)
        {
            foreach (var target in rasters.Where(_ => _ != source))
            {
                registry.Add(new CanvasConversion(source, target, canvas));
            }
        }
    }

    public void Loaded(IServiceProvider services) =>
        canvas.Runtime = services.GetRequiredService<IJSRuntime>();
}
