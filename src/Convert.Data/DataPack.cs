namespace Convert;

/// <summary>
/// Structured data: JSON, YAML, TOML, XML, CSV and TSV, each to every other, through one tree. YAML and
/// TOML routes load their library; the rest are the .NET runtime's own JSON and XML. Also images to a
/// data: URL.
/// </summary>
public sealed class DataPack : IPack
{
    public static Tool DotNet { get; } = new(
        "dotnet",
        ".NET",
        "The runtime's own System.Text.Json and System.Xml, with no extra download.",
        "https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview");

    public static Tool YamlDotNet { get; } = new(
        "yamldotnet",
        "YamlDotNet",
        "A YAML 1.1/1.2 parser and emitter for .NET.",
        "https://github.com/aaubry/YamlDotNet");

    public static Tool Tomlyn { get; } = new(
        "tomlyn",
        "Tomlyn",
        "A TOML 1.0 parser and writer for .NET.",
        "https://github.com/xoofx/Tomlyn");

    internal static IReadOnlyList<FileType> Formats { get; } =
    [
        FileTypes.Json,
        FileTypes.Yaml,
        FileTypes.Toml,
        FileTypes.Xml,
        FileTypes.Csv,
        FileTypes.Tsv
    ];

    /// <summary>The library a format needs, if any.</summary>
    internal static IReadOnlyList<string> AssembliesFor(FileType type) =>
        type.Id switch
        {
            "yaml" => ["YamlDotNet.wasm"],
            "toml" => ["Tomlyn.wasm"],
            _ => []
        };

    public void Register(ConversionRegistry registry)
    {
        foreach (var format in Formats)
        {
            registry.Add(format);
        }

        registry.Add(new Preview(FileTypes.Json, [], typeof(TextPreview)));
        registry.Add(new Preview(FileTypes.Yaml, [], typeof(TextPreview)));
        registry.Add(new Preview(FileTypes.Toml, [], typeof(TextPreview)));
        registry.Add(new Preview(FileTypes.Xml, [], typeof(TextPreview)));
        // A CSV is a table first: GeoConvert's map of a CSV with coordinates comes second.
        registry.Add(new Preview(FileTypes.Csv, [], typeof(TablePreview), Priority: -1));
        registry.Add(new Preview(FileTypes.Tsv, [], typeof(TablePreview)));

        foreach (var source in Formats)
        {
            foreach (var target in Formats.Where(_ => _ != source))
            {
                registry.Add(new DataConversion(source, target));
            }
        }

        // Images a page can show from a data: URL.
        FileType[] embeddable = [FileTypes.Png, FileTypes.Jpeg, FileTypes.WebP, FileTypes.Gif, FileTypes.Svg, FileTypes.Ico, FileTypes.Bmp];
        foreach (var source in embeddable)
        {
            registry.Add(source);
            registry.Add(new DataUriConversion(source));
        }

        registry.Add(new Preview(FileTypes.DataUri, [], typeof(TextPreview)));

        foreach (var format in Formats)
        {
            var file = format.FileNameFor("planets");
            registry.Add(new Sample(format.DisplayName, format.Icon, $"_content/Convert.Data/samples/{file}", file, "Data"));
        }
    }
}
