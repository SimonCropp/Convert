using System.Security;
using System.Text.RegularExpressions;
using Naiad;

namespace Convert;

/// <summary>The only place Naiad's namespace is used. A parse error is the user's input, not a bug.</summary>
static class MermaidRunner
{
    /// <summary>The source laid out by Naiad. Text it cannot lay out, or that would no longer parse, comes back as it is.</summary>
    public static string Format(string mermaid) =>
        Mermaid.Format(mermaid);

    public static string Svg(string mermaid, MermaidOptions options)
    {
        var render = Map(options);
        render.PrettyPrint = options.PrettyPrint;
        var svg = Guard(() => Mermaid.Render(mermaid, render));
        return WithBackground(svg, options.Fill, options.PrettyPrint);
    }

    /// <summary>For drawing outside a browser (Skia): plain-text labels, since there is no HTML layout.</summary>
    public static string PlainSvg(string mermaid, MermaidOptions options)
    {
        var render = Map(options);
        render.AllowHtmlElements = false;
        var svg = Guard(() => Mermaid.Render(mermaid, render));
        return WithBackground(svg, options.Fill, false);
    }

    /// <summary>The preview: offline-safe, with no Font Awesome import, on the default background.</summary>
    public static string Preview(string mermaid)
    {
        var svg = Guard(() => Mermaid.Render(mermaid, new() { AllowHtmlElements = false }));
        return WithBackground(svg, new MermaidOptions().Fill, false);
    }

    /// <summary>
    /// Naiad draws no canvas, so a viewer shows whatever is behind the SVG. A rect over the whole
    /// viewBox, first so it sits under everything. The fill is an inline style so Naiad's own
    /// <c>rect{…}</c> rules can't restyle it.
    /// </summary>
    internal static string WithBackground(string svg, string? fill, bool prettyPrint)
    {
        if (string.IsNullOrWhiteSpace(fill))
        {
            return svg;
        }

        var start = svg.IndexOf("<svg", StringComparison.Ordinal);
        if (start < 0)
        {
            return svg;
        }

        var end = svg.IndexOf('>', start);
        if (end < 0)
        {
            return svg;
        }

        var tag = svg[start..end];
        var match = viewBox.Match(tag);
        if (!match.Success)
        {
            return svg;
        }

        var box = match.Groups["box"].Value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
        if (box.Length != 4)
        {
            return svg;
        }

        var style = $"fill:{SecurityElement.Escape(fill.Trim())};stroke:none;";
        return svg.Insert(end + 1, Rect(box, style, prettyPrint, svg.AsSpan(end + 1)));
    }

    static string Rect(string[] box, string style, bool prettyPrint, CharSpan after)
    {
        var rect = $"<rect x='{box[0]}' y='{box[1]}' width='{box[2]}' height='{box[3]}' style='{style}'/>";
        if (!prettyPrint)
        {
            return rect;
        }

        // On its own line, indented as Naiad's pretty print lays out the root's children.
        var newline = after.StartsWith("\r\n") ? "\r\n" : "\n";
        return $"{newline}  {rect}";
    }

    static readonly Regex viewBox = new("""viewBox\s*=\s*['"](?<box>[^'"]+)['"]""", RegexOptions.Compiled);

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
