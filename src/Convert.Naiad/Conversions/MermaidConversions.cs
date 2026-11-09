namespace Convert;

sealed class MermaidToSvg : Conversion<MermaidOptions>
{
    public override string Id => "mermaid-svg-naiad";
    public override FileType Source => FileTypes.Mermaid;
    public override FileType Target => FileTypes.Svg;
    public override Tool Tool => NaiadPack.Tool;
    public override string Summary => "Vector diagram; flowchart, sequence, class, state, ER, Gantt, pie and more.";
    public override IReadOnlyList<string> Assemblies => NaiadPack.SvgAssemblies;
    public override Type OptionsComponent => typeof(MermaidOptionsPanel);

    public override object CreateOptions() => new MermaidOptions { Svg = true };

    protected override Task<ConversionOutput> RunAsync(Document source, MermaidOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var svg = MermaidRunner.Svg(source.Text, options);
        return Task.FromResult(new ConversionOutput(Document.FromText(FileTypes.Svg.FileNameFor(source.FileName), svg, FileTypes.Svg)));
    }
}

sealed class MermaidToPng : Conversion<MermaidOptions>
{
    public override string Id => "mermaid-png-naiad";
    public override FileType Source => FileTypes.Mermaid;
    public override FileType Target => FileTypes.Png;
    public override Tool Tool => NaiadPack.Tool;
    public override string Summary => "Rasterised by ImageSharp in .NET, at any scale.";
    public override string Expect => "Labels use the bundled font; HTML labels are drawn as plain text.";
    public override IReadOnlyList<string> Assemblies => NaiadPack.PngAssemblies;
    public override Type OptionsComponent => typeof(MermaidOptionsPanel);

    public override object CreateOptions() => new MermaidOptions { Png = true };

    protected override Task<ConversionOutput> RunAsync(Document source, MermaidOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var png = MermaidRunner.Png(source.Text, options);
        return Task.FromResult(new ConversionOutput(new Document(FileTypes.Png.FileNameFor(source.FileName), png, FileTypes.Png)));
    }
}
