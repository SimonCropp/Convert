namespace Convert;

/// <summary>Mermaid diagrams to SVG, PNG and PDF with Naiad, a managed Mermaid renderer (PDF drawn by Skia).</summary>
public sealed class NaiadPack : IPack
{
    public static Tool Tool { get; } = new(
        "naiad",
        "Naiad",
        "Renders Mermaid diagrams to SVG and PNG in .NET, with no browser or Node.",
        "https://github.com/Papyrine/Naiad");

    internal static IReadOnlyList<string> SvgAssemblies { get; } = ["Naiad.wasm", "Parlot.wasm"];

    internal static IReadOnlyList<string> PngAssemblies { get; } =
    [
        .. SvgAssemblies,
        "Naiad.ImageSharp.wasm",
        "SixLabors.ImageSharp.wasm",
        "SixLabors.ImageSharp.Drawing.wasm",
        "SixLabors.Fonts.wasm",
        "SixLabors.PolygonClipper.wasm",
        "System.IO.Hashing.wasm"
    ];

    internal static IReadOnlyList<string> PdfAssemblies { get; } = [.. SvgAssemblies, .. SkiaPack.SvgAssemblies];

    static readonly string[] samples =
    [
        "Flowchart",
        "Sequence",
        "Class",
        "State",
        "Pie",
        "Gantt",
        "Ishikawa",
        "ZenUML",
        "Tree View",
        "Wardley Map",
        "Venn",
        "Swimlane",
        "Event Modeling",
        "Use Case",
        "Cynefin",
        "Agent Flow",
        "Railroad"
    ];

    public void Register(ConversionRegistry registry)
    {
        registry.Add(FileTypes.Mermaid);
        registry.Add(new MermaidToSvg());
        registry.Add(new MermaidToPng());
        registry.Add(new MermaidToPdf());
        registry.Add(new Preview(FileTypes.Mermaid, SvgAssemblies, typeof(MermaidPreview)));
        foreach (var sample in samples)
        {
            var file = sample.ToLowerInvariant().Replace(' ', '-') + ".mmd";
            registry.Add(new Sample(sample, Lucide.Workflow, $"_content/Convert.Naiad/samples/{file}", file, "Mermaid"));
        }
    }

    // Mermaid to PDF reads the Aptos faces through Skia's font loader.
    public void Loaded(IServiceProvider services) =>
        SkiaFonts.Initialize(services.GetRequiredService<HttpClient>());
}
