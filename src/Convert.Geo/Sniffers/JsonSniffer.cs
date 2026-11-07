namespace Convert.Geo;

/// <summary>
/// Tells GeoJSON from TopoJSON in a <c>.json</c> file by the root object's <c>type</c>. BCL only.
/// </summary>
static class JsonSniffer
{
    static readonly HashSet<string> geoJsonTypes =
    [
        "FeatureCollection",
        "Feature",
        "Point",
        "MultiPoint",
        "LineString",
        "MultiLineString",
        "Polygon",
        "MultiPolygon",
        "GeometryCollection"
    ];

    public static FileType? Sniff(string extension, ReadOnlySpan<byte> bytes)
    {
        if (extension != ".json")
        {
            return null;
        }

        return RootType(bytes) switch
        {
            "Topology" => FileTypes.TopoJson,
            { } type when geoJsonTypes.Contains(type) => FileTypes.GeoJson,
            _ => null
        };
    }

    // Reads only the root object's top-level properties, skipping every nested value, so a large file
    // costs one pass of the tokenizer at most.
    static string? RootType(ReadOnlySpan<byte> bytes)
    {
        try
        {
            var reader = new Utf8JsonReader(bytes, new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                return null;
            }

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var isType = reader.ValueTextEquals("type"u8);
                if (!reader.Read())
                {
                    return null;
                }

                if (isType && reader.TokenType == JsonTokenType.String)
                {
                    return reader.GetString();
                }

                reader.Skip();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
