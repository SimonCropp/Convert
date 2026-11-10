/// <summary>HTML to Word with OpenXmlHtml.</summary>
public sealed class OpenXmlHtmlPack : IPack
{
    public static Tool Tool { get; } = new(
        "openxmlhtml",
        "OpenXmlHtml",
        "Converts HTML into native Word paragraphs, tables, lists and styles.",
        "https://github.com/SimonCropp/OpenXmlHtml");

    internal static IReadOnlyList<string> Assemblies { get; } =
    [
        "OpenXmlHtml.wasm",
        "OpenXmlKit.wasm",
        "DocumentFormat.OpenXml.wasm",
        "DocumentFormat.OpenXml.Framework.wasm",
        "System.IO.Packaging.wasm",
        "AngleSharp.wasm"
    ];

    public void Register(ConversionRegistry registry) =>
        registry.Add(new HtmlToDocx());
}
