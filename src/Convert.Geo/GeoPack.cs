namespace Convert;

/// <summary>Geospatial formats through GeoConvert: each to every other, plus PNG and SVG maps.</summary>
public sealed class GeoPack : IPack
{
    public static Tool Tool { get; } = new(
        "geoconvert",
        "GeoConvert",
        "Reads and writes GeoJSON, TopoJSON, KML, GPX, FlatGeobuf, GeoParquet and more, and draws maps.",
        "https://github.com/Papyrine/GeoConvert");

    internal static IReadOnlyList<string> Assemblies { get; } =
    [
        "GeoConvert.wasm", "System.IO.Hashing.wasm"
    ];

    /// <summary>Every format read from a single stream (Shapefile is path-based and multi-file, so excluded).</summary>
    internal static IReadOnlyList<FileType> Formats { get; } =
    [
        FileTypes.GeoJson,
        FileTypes.TopoJson,
        FileTypes.Kml,
        FileTypes.Kmz,
        FileTypes.Gpx,
        FileTypes.Wkt,
        FileTypes.Wkb,
        FileTypes.Csv,
        FileTypes.FlatGeobuf,
        FileTypes.GeoParquet
    ];

    public void Register(ConversionRegistry registry)
    {
        foreach (var format in Formats)
        {
            registry.Add(format);
            registry.Add(new Preview(format, Assemblies, typeof(MapPreview), Accepts: GeoCsv.Accepts));
        }

        foreach (var source in Formats)
        {
            foreach (var target in Formats.Where(_ => _ != source))
            {
                registry.Add(new GeoConversion(source, target));
            }

            registry.Add(new GeoRenderConversion(source, FileTypes.Png));
            registry.Add(new GeoRenderConversion(source, FileTypes.Svg));
        }

        registry.Add(JsonSniffer.Sniff);
        // The same capitals in every format (made by GeoSampleTests).
        foreach (var format in Formats)
        {
            var file = format.FileNameFor("capitals");
            registry.Add(new Sample(format.DisplayName, Lucide.Globe, $"_content/Convert.Geo/samples/{file}", file, "Map"));
        }
    }
}
