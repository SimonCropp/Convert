using Naiad;

namespace Convert.Naiad;

/// <summary>The only place Naiad's namespace is used. A parse error is the user's input, not a bug.</summary>
static class MermaidRunner
{
    public static string Svg(string mermaid, MermaidOptions options) =>
        Guard(() => Mermaid.Render(mermaid, Map(options)));

    public static byte[] Png(string mermaid, MermaidOptions options)
    {
        var render = Map(options);
        // Drawn by ImageSharp, which has no HTML layout: plain-text labels.
        render.AllowHtmlElements = false;
        render.Png.Scale = options.PngScale;
        render.Png.Background = options.PngBackground;
        return Guard(() => ImageSharpRenderer.RenderPng(mermaid, render));
    }

    /// <summary>The preview: offline-safe, with no Font Awesome import.</summary>
    public static string Preview(string mermaid) =>
        Guard(() => Mermaid.Render(mermaid, new() { AllowHtmlElements = false }));

    static RenderOptions Map(MermaidOptions options) =>
        new()
        {
            Padding = options.Padding,
            FontSize = options.FontSize,
            FontFamily = options.FontFamily,
            AllowHtmlElements = options.AllowHtmlElements
        };

    static T Guard<T>(Func<T> render)
    {
        try
        {
            return render();
        }
        catch (MermaidParseException exception)
        {
            throw new UserInputException($"The diagram could not be parsed: {exception.Message}", exception);
        }
        catch (MermaidException exception)
        {
            throw new UserInputException(exception.Message, exception);
        }
    }
}
