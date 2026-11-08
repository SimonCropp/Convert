namespace Convert;

/// <summary>Mirrors GeoConvert's <c>MapProjection</c>.</summary>
public enum GeoProjection
{
    Auto,
    PlateCarree,
    WebMercator,
    Lambert,
    Goode
}

/// <summary>Mirrors GeoConvert's <c>ParquetCompression</c>.</summary>
public enum GeoParquetCodec
{
    Snappy,
    Gzip,
    Uncompressed
}

/// <summary>
/// Map render settings, mirroring GeoConvert's <c>RenderOptions</c> with BCL types. Colours are
/// <c>#rrggbb</c> plus a 0–255 opacity, because <c>&lt;input type="color"&gt;</c> has no alpha.
/// </summary>
public sealed class GeoRenderOptions
{
    public GeoProjection Projection { get; set; } = GeoProjection.Auto;
    public int MaxDimension { get; set; } = 2048;
    public int Padding { get; set; } = 8;

    public int StrokeWidth { get; set; } = 2;
    public int PointRadius { get; set; } = 4;
    public bool StrokeAutoScale { get; set; } = true;
    public double MinFeaturePixels { get; set; } = 1;

    public bool Labels { get; set; }
    public double LabelSize { get; set; } = 14;

    public string Background { get; set; } = "#ffffff";
    public bool OceanEnabled { get; set; } = true;
    public string Ocean { get; set; } = "#c8dcf0";
    public string Stroke { get; set; } = "#1e1e1e";
    public string Fill { get; set; } = "#4682b4";
    public int FillOpacity { get; set; } = 120;
    public string LabelColor { get; set; } = "#141414";

    /// <summary>SVG only: Douglas–Peucker tolerance in pixels; 0 keeps every vertex.</summary>
    public double SvgSimplifyTolerance { get; set; }

    /// <summary>SVG only: indent the markup, one element per line.</summary>
    public bool PrettyPrint { get; set; } = true;

    /// <summary>Set by the conversion: show the SVG-only setting.</summary>
    public bool Svg { get; set; }
}

public sealed class KmzOptions
{
    public CompressionLevel Compression { get; set; } = CompressionLevel.Optimal;
}

public sealed class GeoParquetOptions
{
    public GeoParquetCodec Codec { get; set; } = GeoParquetCodec.Snappy;
    public CompressionLevel GzipLevel { get; set; } = CompressionLevel.Optimal;
}

static class GeoChoices
{
    public static readonly IReadOnlyList<(CompressionLevel Value, string Label)> Compression =
    [
        (CompressionLevel.Optimal, "Optimal — default"),
        (CompressionLevel.Fastest, "Fastest"),
        (CompressionLevel.SmallestSize, "Smallest"),
        (CompressionLevel.NoCompression, "None")
    ];

    public static readonly IReadOnlyList<(GeoParquetCodec Value, string Label)> Codecs =
    [
        (GeoParquetCodec.Snappy, "Snappy — default"),
        (GeoParquetCodec.Gzip, "GZIP — smaller"),
        (GeoParquetCodec.Uncompressed, "Uncompressed")
    ];

    public static readonly IReadOnlyList<(GeoProjection Value, string Label)> Projections =
    [
        (GeoProjection.Auto, "Automatic"),
        (GeoProjection.PlateCarree, "Plate carrée (equirectangular)"),
        (GeoProjection.WebMercator, "Web Mercator"),
        (GeoProjection.Lambert, "Lambert conformal conic"),
        (GeoProjection.Goode, "Goode homolosine")
    ];
}
