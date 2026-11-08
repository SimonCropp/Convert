namespace Convert;

/// <summary>
/// Reads with one format and writes with another. Each format is its own class, so a route's first call
/// resolves only its two formats' libraries (Mono resolves a method's locals when it first runs it).
/// </summary>
static class DataRunner
{
    public static string Convert(string text, FileType source, FileType target, DataOptions options)
    {
        var tree = Read(text, source, options);
        return Write(tree, target, options);
    }

    static JsonNode? Read(string text, FileType source, DataOptions options) =>
        source.Id switch
        {
            "json" => JsonFormat.Read(text),
            "yaml" => YamlFormat.Read(text),
            "toml" => TomlFormat.Read(text),
            "xml" => XmlFormat.Read(text, options.InferTypes),
            "csv" or "tsv" => DelimitedFormat.Read(text, Delimited.For(source), options.InferTypes),
            _ => throw new NotSupportedException($"{source.DisplayName} is not a data format.")
        };

    static string Write(JsonNode? tree, FileType target, DataOptions options) =>
        target.Id switch
        {
            "json" => JsonFormat.Write(tree, options.PrettyPrint),
            "yaml" => YamlFormat.Write(tree),
            "toml" => TomlFormat.Write(tree),
            "xml" => XmlFormat.Write(tree, options.PrettyPrint),
            "csv" or "tsv" => DelimitedFormat.Write(tree, Delimited.For(target)),
            _ => throw new NotSupportedException($"{target.DisplayName} is not a data format.")
        };
}
