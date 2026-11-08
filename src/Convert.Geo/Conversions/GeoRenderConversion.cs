namespace Convert;

/// <summary>A geospatial file drawn as a map.</summary>
sealed class GeoRenderConversion(FileType source, FileType target) : Conversion<GeoRenderOptions>
{
    public override string Id => $"{source.Id}-{target.Id}-geoconvert";
    public override FileType Source => source;
    public override bool Accepts(Document document) => GeoCsv.Accepts(document);
    public override FileType Target => target;
    public override Tool Tool => GeoPack.Tool;

    public override string Summary
    {
        get
        {
            if (target == FileTypes.Svg)
            {
                return "A vector map: every feature as a path.";
            }

            return "A raster map, drawn in .NET.";
        }
    }

    public override IReadOnlyList<string> Assemblies => GeoPack.Assemblies;
    public override Type OptionsComponent => typeof(GeoRenderOptionsPanel);

    public override object CreateOptions() => new GeoRenderOptions { Svg = target == FileTypes.Svg };

    protected override Task<ConversionOutput> RunAsync(Document document, GeoRenderOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var bytes = GeoRunner.Render(document, target, options, progress);
        Document part = new(target.FileNameFor(document.FileName), bytes, target);
        if (options.Svg)
        {
            part = PrettyPrint.Apply(part, options.PrettyPrint);
        }

        return Task.FromResult(new ConversionOutput(part));
    }
}