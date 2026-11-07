using System.Xml;

namespace Convert;

/// <summary>The only place GeoConvert's namespace is used. Stream overloads only: no filesystem here.</summary>
static class GeoRunner
{
    public static byte[] Convert(Document document, FileType target, object options, IProgress<ConversionProgress> progress)
    {
        var features = Read(document, progress);
        progress.Report(new("Writing", target.DisplayName));
        using var stream = new MemoryStream();
        switch (options)
        {
            case KmzOptions kmz:
                Kmz.Write(stream, features, kmz.Compression);
                break;
            case GeoParquetOptions parquet:
                GeoParquet.Write(stream, features, Codec(parquet.Codec), parquet.GzipLevel);
                break;
            default:
                GeoConverter.Write(features, stream, Format(target));
                break;
        }

        return stream.ToArray();
    }

    public static byte[] Render(Document document, FileType target, GeoRenderOptions options, IProgress<ConversionProgress> progress)
    {
        var features = Read(document, progress);
        progress.Report(new("Drawing", $"{features.Count:N0} features"));
        var render = Map(options);
        return target == FileTypes.Svg
            ? Encoding.UTF8.GetBytes(MapRenderer.RenderSvg(features, render))
            : MapRenderer.RenderPng([features], render);
    }

    /// <summary>The preview: a small SVG, or a PNG when there are too many features for a DOM.</summary>
    public static Document Preview(Document document)
    {
        var features = Read(document, null);
        var render = Map(new() { MaxDimension = 1200 });
        if (features.Count > 20_000)
        {
            return new(FileTypes.Png.FileNameFor(document.FileName), MapRenderer.RenderPng([features], render), FileTypes.Png);
        }

        return Document.FromText(FileTypes.Svg.FileNameFor(document.FileName), MapRenderer.RenderSvg(features, render), FileTypes.Svg);
    }

    static FeatureCollection Read(Document document, IProgress<ConversionProgress>? progress)
    {
        progress?.Report(new("Reading", document.Type.DisplayName));
        using var stream = new MemoryStream(document.Bytes);
        try
        {
            return GeoConverter.Read(stream, Format(document.Type));
        }
        catch (Exception exception)
            when (exception is
                      FormatException or
                      InvalidDataException or
                      JsonException or
                      XmlException)
        {
            throw new UserInputException($"The {document.Type.DisplayName} file could not be read: {exception.Message}", exception);
        }
    }

    static GeoFormat Format(FileType type) =>
        type.Id switch
        {
            "geojson" => GeoFormat.GeoJson,
            "topojson" => GeoFormat.TopoJson,
            "kml" => GeoFormat.Kml,
            "kmz" => GeoFormat.Kmz,
            "gpx" => GeoFormat.Gpx,
            "wkt" => GeoFormat.Wkt,
            "wkb" => GeoFormat.Wkb,
            "csv" => GeoFormat.Csv,
            "fgb" => GeoFormat.FlatGeobuf,
            "geoparquet" => GeoFormat.GeoParquet,
            _ => throw new NotSupportedException($"GeoConvert does not handle {type.DisplayName}.")
        };

    // Mapped by name, not by value: the mirrors' member order is not the library's.
    static ParquetCompression Codec(GeoParquetCodec codec) =>
        codec switch
        {
            GeoParquetCodec.Gzip => ParquetCompression.Gzip,
            GeoParquetCodec.Uncompressed => ParquetCompression.Uncompressed,
            _ => ParquetCompression.Snappy
        };

    static MapProjection Projection(GeoProjection projection) =>
        projection switch
        {
            GeoProjection.PlateCarree => MapProjection.PlateCarree,
            GeoProjection.WebMercator => MapProjection.WebMercator,
            GeoProjection.Lambert => MapProjection.Lambert,
            GeoProjection.Goode => MapProjection.Goode,
            _ => MapProjection.Auto
        };

    // Common name-like keys, tried in order, so labels work without naming a property.
    static readonly string[] labelKeys = ["name", "NAME", "Name", "name_en", "NAME_EN", "admin", "ADMIN", "title", "label", "id"];

    static string? LabelFor(Feature feature)
    {
        foreach (var key in labelKeys)
        {
            if (feature.Properties.TryGetValue(key, out var value) &&
                value?.ToString() is { Length: > 0 } text &&
                !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return feature.Id?.ToString();
    }

    static RenderOptions Map(GeoRenderOptions options)
    {
        var render = new RenderOptions
        {
            Projection = Projection(options.Projection),
            Padding = options.Padding,
            Background = Color(options.Background, 255),
            Stroke = Color(options.Stroke, 255),
            Fill = Color(options.Fill, options.FillOpacity),
            StrokeWidth = options.StrokeWidth,
            PointRadius = options.PointRadius,
            StrokeAutoScale = options.StrokeAutoScale,
            LabelSize = options.LabelSize,
            LabelColor = Color(options.LabelColor, 255),
            Ocean = options.OceanEnabled ? Color(options.Ocean, 255) : null,
            MinFeaturePixels = options.MinFeaturePixels,
            MaxDimension = options.MaxDimension,
            Svg = new() { SimplifyTolerance = options.SvgSimplifyTolerance }
        };
        if (options.Labels)
        {
            render.Label = LabelFor;
        }

        return render;
    }

    static Rgba Color(string hex, int alpha)
    {
        var span = hex.AsSpan().TrimStart('#');
        var opacity = (byte) Math.Clamp(alpha, 0, 255);
        if (span.Length != 6 ||
            !byte.TryParse(span[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red) ||
            !byte.TryParse(span[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green) ||
            !byte.TryParse(span[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
        {
            return new(0, 0, 0, opacity);
        }

        return new(red, green, blue, opacity);
    }
}
