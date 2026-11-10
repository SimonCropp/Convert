/// <summary>Mermaid diagrams to SVG, PNG and PDF with Naiad, a managed Mermaid renderer (PNG and PDF drawn by Skia).</summary>
public sealed class NaiadPack : IPack
{
    public static Tool Tool { get; } = new(
        "naiad",
        "Naiad",
        "Renders Mermaid diagrams to SVG and PNG in .NET, with no browser or Node.",
        "https://github.com/Papyrine/Naiad");

    internal static IReadOnlyList<string> SvgAssemblies { get; } = ["Naiad.wasm", "Parlot.wasm"];

    // PNG and PDF: Naiad's SVG drawn by Svg.Skia with the bundled Aptos. Naiad.ImageSharp is not used: it reads
    // only system fonts, and the browser has none.
    internal static IReadOnlyList<string> SkiaAssemblies { get; } = [.. SvgAssemblies, .. SkiaPack.SvgAssemblies];

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
        registry.Add(new Formatter(FileTypes.Mermaid, SvgAssemblies, MermaidRunner.Format));
        foreach (var sample in samples)
        {
            var file = sample.ToLowerInvariant().Replace(' ', '-') + ".mmd";
            registry.Add(new Sample(sample, Lucide.Workflow, $"_content/Convert.Naiad/samples/{file}", file, "Mermaid"));
        }
    }

    // Mermaid to PNG and PDF read the Aptos faces through Skia's font loader.
    public void Loaded(IServiceProvider services) =>
        SkiaFonts.Initialize(services.GetRequiredService<HttpClient>());
}
