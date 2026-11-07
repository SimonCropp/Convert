namespace Convert.Naiad;

/// <summary>Mermaid diagrams to SVG and PNG with Naiad, a managed Mermaid renderer.</summary>
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

    static readonly string[] samples = ["Flowchart", "Sequence", "Class", "State", "Pie", "Gantt"];

    public void Register(ConversionRegistry registry)
    {
        registry.Add(FileTypes.Mermaid);
        registry.Add(new MermaidToSvg());
        registry.Add(new MermaidToPng());
        registry.Add(new Preview(FileTypes.Mermaid, SvgAssemblies, typeof(Previews.MermaidPreview)));
        foreach (var sample in samples)
        {
            var file = sample.ToLowerInvariant() + ".mmd";
            registry.Add(new Sample(sample, Lucide.Workflow, $"_content/Convert.Naiad/samples/{file}", file, "Mermaid"));
        }
    }
}
