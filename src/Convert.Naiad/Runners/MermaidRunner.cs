using Naiad;

namespace Convert;

/// <summary>The only place Naiad's namespace is used. A parse error is the user's input, not a bug.</summary>
static class MermaidRunner
{
    public static string Svg(string mermaid, MermaidOptions options)
    {
        var render = Map(options);
        render.PrettyPrint = options.PrettyPrint;
        return Guard(() => Mermaid.Render(mermaid, render));
    }

    /// <summary>For drawing outside a browser (Skia): plain-text labels, since there is no HTML layout.</summary>
    public static string PlainSvg(string mermaid, MermaidOptions options)
    {
        var render = Map(options);
        render.AllowHtmlElements = false;
        return Guard(() => Mermaid.Render(mermaid, render));
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
