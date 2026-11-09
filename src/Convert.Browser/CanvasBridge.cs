namespace Convert;

/// <summary>The pack's JavaScript module, <c>_content/Convert.Browser/browser.js</c>, imported on first use.</summary>
sealed class CanvasBridge
{
    public IJSRuntime? Runtime { get; set; }

    Task<IJSObjectReference> Module
    {
        get
        {
            var runtime = Runtime ?? throw new InvalidOperationException("The browser pack has not been loaded.");
            return field ??= runtime.InvokeAsync<IJSObjectReference>("import", "./_content/Convert.Browser/browser.js").AsTask();
        }
    }

    /// <summary>Draws an image (raster or SVG) on a canvas and encodes it as <paramref name="targetType"/>.</summary>
    public async Task<byte[]> RasterizeAsync(byte[] bytes, string sourceType, string targetType, RasterOptions options, Cancel cancel)
    {
        var module = await Module;
        var background = options.Flatten;
        return await module.InvokeAsync<byte[]>(
            "rasterize",
            cancel,
            bytes,
            sourceType,
            targetType,
            options.Scale,
            background,
            options.Quality / 100d);
    }
}
