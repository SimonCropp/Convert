/// <summary>
/// For conversions whose only setting is the layout of their text output (JSON, XML, HTML, SVG). Packs
/// with options of their own carry a <c>PrettyPrint</c> flag there instead. See <see cref="PrettyPrint"/>.
/// </summary>
public sealed class PrettyPrintOptions
{
    /// <summary>Indent the output, one element per line.</summary>
    public bool PrettyPrint { get; set; } = true;
}
