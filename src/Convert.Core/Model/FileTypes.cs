namespace Convert;

/// <summary>
/// Every format any pack reads or writes. Owned by Core so two packs that emit the same format agree on
/// its identity; a pack <em>registers</em> the types it reads (see <see cref="ConversionRegistry.Add(FileType)"/>).
/// </summary>
public static class FileTypes
{
    public static readonly FileType Docx = new("docx", "Word document", [".docx"], "application/vnd.openxmlformats-officedocument.wordprocessingml.document", FileKind.Binary, Lucide.FileText, FileGroup.Documents);
    public static readonly FileType Xlsx = new("xlsx", "Excel workbook", [".xlsx"], "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileKind.Binary, Lucide.FileSpreadsheet, FileGroup.Documents, "sheet");
    public static readonly FileType Pptx = new("pptx", "PowerPoint presentation", [".pptx"], "application/vnd.openxmlformats-officedocument.presentationml.presentation", FileKind.Binary, Lucide.Presentation, FileGroup.Documents, "slide");
    public static readonly FileType Pub = new("pub", "Publisher publication", [".pub"], "application/x-mspublisher", FileKind.Binary, Lucide.Newspaper, FileGroup.Documents);
    public static readonly FileType Pdf = new("pdf", "PDF", [".pdf"], "application/pdf", FileKind.Binary, Lucide.FileType2, FileGroup.Documents);
    public static readonly FileType Html = new("html", "HTML", [".html", ".htm"], "text/html", FileKind.Text, Lucide.FileCode, FileGroup.Text);
    public static readonly FileType Markdown = new("md", "Markdown", [".md", ".markdown"], "text/markdown", FileKind.Text, Lucide.FileText, FileGroup.Text);
    public static readonly FileType Text = new("txt", "Plain text", [".txt"], "text/plain", FileKind.Text, Lucide.Type, FileGroup.Text);
    public static readonly FileType Mermaid = new("mermaid", "Mermaid diagram", [".mmd", ".mermaid"], "text/vnd.mermaid", FileKind.Text, Lucide.Workflow, FileGroup.Text);
    public static readonly FileType Svg = new("svg", "SVG image", [".svg"], "image/svg+xml", FileKind.Binary, Lucide.Shapes, FileGroup.Images);
    public static readonly FileType Png = new("png", "PNG image", [".png"], "image/png", FileKind.Binary, Lucide.Image, FileGroup.Images);
    public static readonly FileType Jpeg = new("jpeg", "JPEG image", [".jpg", ".jpeg"], "image/jpeg", FileKind.Binary, Lucide.FileImage, FileGroup.Images);
    public static readonly FileType WebP = new("webp", "WebP image", [".webp"], "image/webp", FileKind.Binary, Lucide.FileImage, FileGroup.Images);
    public static readonly FileType GeoJson = new("geojson", "GeoJSON", [".geojson"], "application/geo+json", FileKind.Text, Lucide.Globe, FileGroup.Data);
    public static readonly FileType TopoJson = new("topojson", "TopoJSON", [".topojson"], "application/json", FileKind.Text, Lucide.Globe, FileGroup.Data);
    public static readonly FileType Kml = new("kml", "KML", [".kml"], "application/vnd.google-earth.kml+xml", FileKind.Text, Lucide.MapPin, FileGroup.Data);
    public static readonly FileType Kmz = new("kmz", "KMZ", [".kmz"], "application/vnd.google-earth.kmz", FileKind.Binary, Lucide.MapPin, FileGroup.Data);
    public static readonly FileType Gpx = new("gpx", "GPX", [".gpx"], "application/gpx+xml", FileKind.Text, Lucide.Route, FileGroup.Data);
    public static readonly FileType Wkt = new("wkt", "WKT", [".wkt"], "text/plain", FileKind.Text, Lucide.Map, FileGroup.Data);
    public static readonly FileType Wkb = new("wkb", "WKB", [".wkb"], "application/octet-stream", FileKind.Binary, Lucide.Map, FileGroup.Data);
    public static readonly FileType Csv = new("csv", "CSV", [".csv"], "text/csv", FileKind.Text, Lucide.Table, FileGroup.Data);
    public static readonly FileType FlatGeobuf = new("fgb", "FlatGeobuf", [".fgb"], "application/octet-stream", FileKind.Binary, Lucide.Map, FileGroup.Data);
    public static readonly FileType GeoParquet = new("geoparquet", "GeoParquet", [".parquet", ".geoparquet"], "application/vnd.apache.parquet", FileKind.Binary, Lucide.Sheet, FileGroup.Data);
    public static readonly FileType Zip = new("zip", "ZIP archive", [".zip"], "application/zip", FileKind.Binary, Lucide.FileArchive, FileGroup.Data);
}
