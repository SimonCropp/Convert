namespace Convert;

/// <summary>docx/xlsx/pptx to png/pdf/html/md/txt through Morph's ConversionService.</summary>
sealed class MorphConversion(FileType source, FileType target) : Conversion
{
    public override string Id => $"{source.Id}-{target.Id}-morph";
    public override FileType Source => source;
    public override FileType Target => target;
    public override Tool Tool => MorphPack.Tool;

    public override string Summary =>
        target.Id switch
        {
            "png" => $"Each {source.PageNoun} rendered as Office lays it out.",
            "pdf" => "Vector PDF with selectable text and embedded fonts.",
            "html" => "Self-contained HTML: styles inline, images embedded.",
            "md" => "Headings, lists, tables and emphasis as Markdown.",
            _ => "The text content, in reading order."
        };

    public override string? Expect =>
        target.Id switch
        {
            "png" => $"Several {source.PageNoun}s download as a zip of page_0001.png, page_0002.png, ….",
            "pdf" => "Fonts the file names are substituted with the bundled Aptos.",
            "md" or "txt" => "Images and layout are dropped.",
            _ => null
        };

    public override IReadOnlyList<string> Assemblies => MorphPack.Assemblies;

    public override Type? OptionsComponent =>
        target.Id switch
        {
            "png" => typeof(MorphPngOptionsPanel),
            "html" => typeof(PrettyPrintOptionsPanel),
            _ => null
        };

    public override int Priority => 0;

    public override object CreateOptions() =>
        target.Id switch
        {
            "png" => new MorphPngOptions(),
            "html" => new PrettyPrintOptions(),
            _ => new NoOptions()
        };

    public override async Task<ConversionOutput> RunAsync(Document document, object options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var output = await MorphRunner.RunAsync(document, target, options as MorphPngOptions, progress, cancel);
        return options is PrettyPrintOptions pretty ? PrettyPrint.Apply(output, pretty.PrettyPrint) : output;
    }
}
