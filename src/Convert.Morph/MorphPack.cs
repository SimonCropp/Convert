namespace Convert.Morph;

/// <summary>Word, Excel and PowerPoint through Morph: the viewer, the docx editor and 15 conversions.</summary>
public sealed class MorphPack : IPack
{
    public static Tool Tool { get; } = new(
        "morph",
        "Morph",
        "A managed Office renderer: lays out Word, Excel and PowerPoint like Office does.",
        "https://github.com/Papyrine/Morph");

    // Every entry loads Morph.Blazor, so Loaded may register its services (see MorphServices).
    internal static IReadOnlyList<string> Assemblies { get; } =
    [
        "Morph.Blazor.wasm",
        "Morph.wasm",
        "Morph.ImageSharp.wasm",
        "Morph.Pdf.wasm",
        "Krilla.wasm",
        "DocumentFormat.OpenXml.wasm",
        "DocumentFormat.OpenXml.Framework.wasm",
        "System.IO.Packaging.wasm",
        "AngleSharp.wasm",
        "SixLabors.ImageSharp.wasm",
        "SixLabors.ImageSharp.Drawing.wasm",
        "SixLabors.Fonts.wasm",
        "SixLabors.PolygonClipper.wasm",
        "System.IO.Hashing.wasm"
    ];

    public void Register(ConversionRegistry registry)
    {
        FileType[] sources = [FileTypes.Docx, FileTypes.Xlsx, FileTypes.Pptx];
        FileType[] targets = [FileTypes.Png, FileTypes.Pdf, FileTypes.Html, FileTypes.Markdown, FileTypes.Text];
        foreach (var source in sources)
        {
            registry.Add(source);
            registry.Add(new Preview(source, Assemblies, typeof(Previews.MorphPreview)));
            foreach (var target in targets)
            {
                registry.Add(new MorphConversion(source, target));
            }

            registry.Add(new Sample(source.DisplayName, source.Icon, $"_content/Morph.Blazor/sample/sample{source.Extension}", $"sample{source.Extension}", "MS Office"));
        }

        registry.Add(new Editor(FileTypes.Docx, Assemblies, typeof(Editors.DocxEditor)));
    }

    public void Loaded(IServiceProvider services) =>
        MorphServices.Register(services);
}
