namespace Convert;

/// <summary>Mirrors Naiad's <c>RenderOptions</c> with BCL types only.</summary>
public sealed class MermaidOptions
{
    public double Padding { get; set; } = 20;

    public int FontSize { get; set; } = 14;

    public string FontFamily { get; set; } = "Arial, sans-serif";

    /// <summary>
    /// HTML labels inside <c>&lt;foreignObject&gt;</c>. Richer text, but such an SVG can't be drawn on a
    /// canvas and it imports Font Awesome from a CDN.
    /// </summary>
    public bool AllowHtmlElements { get; set; }

    /// <summary>SVG only: indent the markup, one element per line (Naiad's <c>PrettyPrint</c>).</summary>
    public bool PrettyPrint { get; set; } = true;

    public double PngScale { get; set; } = 2;

    public string PngBackground { get; set; } = "#ffffff";

    /// <summary>Set by the conversion: show the PNG-only settings.</summary>
    public bool Png { get; set; }

    /// <summary>Set by the conversion: show the SVG-only settings (layout, HTML labels).</summary>
    public bool Svg { get; set; }
}
