namespace Convert;

/// <summary>Markdown to HTML with Markdig, and the rendered Markdown preview.</summary>
public sealed class MarkdigPack : IPack
{
    public static Tool Tool { get; } = new(
        "markdig",
        "Markdig",
        "A fast, CommonMark-compliant Markdown processor for .NET.",
        "https://github.com/xoofx/markdig");

    internal static IReadOnlyList<string> Assemblies { get; } = ["Markdig.wasm"];

    public void Register(ConversionRegistry registry)
    {
        registry.Add(new MarkdownToHtml());
        registry.Add(new Preview(FileTypes.Markdown, Assemblies, typeof(MarkdownPreview)));
        registry.Add(new Sample("Markdown", Lucide.FileText, "_content/Convert.Markdig/samples/readme.md", "readme.md"));
    }
}
