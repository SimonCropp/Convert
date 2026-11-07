namespace Convert;

/// <summary>One geospatial format to another: read to features, write the features.</summary>
sealed class GeoConversion(FileType source, FileType target) : Conversion
{
    public override string Id => $"{source.Id}-{target.Id}-geoconvert";
    public override FileType Source => source;
    public override FileType Target => target;
    public override Tool Tool => GeoPack.Tool;
    public override string Summary => "Geometry and properties carried feature by feature.";

    public override string? Expect =>
        target.Id switch
        {
            "gpx" => "GPX holds points, routes and tracks: polygons are written as their outlines.",
            "wkt" or "wkb" => "WKT/WKB hold geometry only: properties are dropped.",
            "csv" => "CSV keeps one geometry per row as WKT, with properties as columns.",
            _ => null
        };

    public override IReadOnlyList<string> Assemblies => GeoPack.Assemblies;

    public override Type? OptionsComponent =>
        target.Id switch
        {
            "kmz" => typeof(KmzOptionsPanel),
            "geoparquet" => typeof(GeoParquetOptionsPanel),
            _ => null
        };

    public override object CreateOptions() =>
        target.Id switch
        {
            "kmz" => new KmzOptions(),
            "geoparquet" => new GeoParquetOptions(),
            _ => new NoOptions()
        };

    public override Task<ConversionOutput> RunAsync(Document document, object options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var bytes = GeoRunner.Convert(document, target, options, progress);
        return Task.FromResult(
            new ConversionOutput(
                new(
                    target.FileNameFor(document.FileName),
                    bytes,
                    target)));
    }
}