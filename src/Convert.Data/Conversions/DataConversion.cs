namespace Convert;

/// <summary>One data format read into a tree and written out as another.</summary>
sealed class DataConversion(FileType source, FileType target) : Conversion<DataOptions>
{
    public override string Id => $"{source.Id}-{target.Id}-{Tool.Id}";
    public override FileType Source => source;
    public override FileType Target => target;

    // The library the route loads; TOML's when both are YAML and TOML.
    public override Tool Tool =>
        Uses(FileTypes.Toml)
            ? DataPack.Tomlyn
            : Uses(FileTypes.Yaml)
                ? DataPack.YamlDotNet
                : DataPack.DotNet;

    public override string Summary =>
        Uses(FileTypes.Toml) && Uses(FileTypes.Yaml)
            ? "Read by YamlDotNet and written by Tomlyn, or the reverse, through one tree."
            : "Read into a tree of objects, lists and values, and written back out.";

    public override string? Expect
    {
        get
        {
            var notes = new List<string>();
            if (DataOptions.IsUntyped(source))
            {
                notes.Add(source == FileTypes.Xml
                    ? "Attributes become @-keys, text beside elements #text, and repeated elements lists."
                    : "Each row becomes an object keyed by the header row.");
            }

            switch (target.Id)
            {
                case "csv" or "tsv":
                    notes.Add("Nested objects become dotted columns (address.city); lists are written as JSON.");
                    break;
                case "toml":
                    notes.Add("TOML has no null, so null values are left out; a top-level list goes under \"items\".");
                    break;
                case "xml":
                    notes.Add("@-keys become attributes and lists repeated elements; XML keeps no number or true/false types.");
                    break;
            }

            return notes.Count == 0 ? null : string.Join(' ', notes);
        }
    }

    public override IReadOnlyList<string> Assemblies =>
        [.. DataPack.AssembliesFor(source).Concat(DataPack.AssembliesFor(target)).Distinct(StringComparer.Ordinal)];

    public override Type? OptionsComponent =>
        DataOptions.IsUntyped(source) || target == FileTypes.Json ? typeof(DataOptionsPanel) : null;

    public override object CreateOptions() =>
        new DataOptions
        {
            ShowInference = DataOptions.IsUntyped(source),
            ShowIndent = target == FileTypes.Json
        };

    protected override Task<ConversionOutput> RunAsync(Document document, DataOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var text = DataRunner.Convert(document.Text, source, target, options);
        return Task.FromResult(
            new ConversionOutput(
                Document.FromText(
                    target.FileNameFor(document.FileName),
                    text,
                    target)));
    }

    bool Uses(FileType type) =>
        source == type || target == type;
}
