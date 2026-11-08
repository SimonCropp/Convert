namespace Convert;

/// <summary>
/// Whether a CSV holds geometry GeoConvert can read: a WKT column, or longitude and latitude columns.
/// A CSV without them is a plain table, so the map preview and geo targets don't apply. BCL only.
/// </summary>
static class GeoCsv
{
    static readonly HashSet<string> wkt = new(["wkt", "geometry", "geom", "the_geom"], StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<string> longitude = new(["x", "lon", "lng", "longitude"], StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<string> latitude = new(["y", "lat", "latitude"], StringComparer.OrdinalIgnoreCase);

    public static bool HasGeometry(Document document)
    {
        var rows = Delimited.Read(document.Text, Delimited.For(document.Type), maxRecords: 1);
        if (rows.Count == 0)
        {
            return false;
        }

        var header = rows[0].Select(_ => _.Trim()).ToList();
        if (header.Any(wkt.Contains))
        {
            return true;
        }

        return header.Any(longitude.Contains) && header.Any(latitude.Contains);
    }

    /// <summary>True for every format but CSV, and for a CSV with geometry.</summary>
    public static bool Accepts(Document document) =>
        document.Type != FileTypes.Csv || HasGeometry(document);
}
