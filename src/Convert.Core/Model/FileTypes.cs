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
    public static readonly FileType Gif = new("gif", "GIF image", [".gif"], "image/gif", FileKind.Binary, Lucide.FileImage, FileGroup.Images);
    public static readonly FileType Bmp = new("bmp", "BMP image", [".bmp", ".dib"], "image/bmp", FileKind.Binary, Lucide.FileImage, FileGroup.Images);
    public static readonly FileType Tiff = new("tiff", "TIFF image", [".tiff", ".tif"], "image/tiff", FileKind.Binary, Lucide.FileImage, FileGroup.Images);
    public static readonly FileType Ico = new("ico", "Icon", [".ico"], "image/x-icon", FileKind.Binary, Lucide.FileImage, FileGroup.Images);
    public static readonly FileType Cur = new("cur", "Cursor", [".cur"], "image/x-win-bitmap", FileKind.Binary, Lucide.MousePointer2, FileGroup.Images);
    public static readonly FileType Ani = new("ani", "Animated cursor", [".ani"], "application/x-navi-animation", FileKind.Binary, Lucide.MousePointer2, FileGroup.Images);
    public static readonly FileType Tga = new("tga", "TGA image", [".tga"], "image/x-tga", FileKind.Binary, Lucide.FileImage, FileGroup.Images);
    public static readonly FileType Qoi = new("qoi", "QOI image", [".qoi"], "image/qoi", FileKind.Binary, Lucide.FileImage, FileGroup.Images);
    public static readonly FileType Netpbm = new("pnm", "Netpbm image", [".ppm", ".pgm", ".pbm", ".pnm"], "image/x-portable-anymap", FileKind.Binary, Lucide.FileImage, FileGroup.Images);
    public static readonly FileType Exr = new("exr", "OpenEXR image", [".exr"], "image/x-exr", FileKind.Binary, Lucide.FileImage, FileGroup.Images);
    public static readonly FileType Wbmp = new("wbmp", "WBMP image", [".wbmp"], "image/vnd.wap.wbmp", FileKind.Binary, Lucide.FileImage, FileGroup.Images);
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
    public static readonly FileType Tsv = new("tsv", "TSV", [".tsv", ".tab"], "text/tab-separated-values", FileKind.Text, Lucide.Table, FileGroup.Data);
    public static readonly FileType Json = new("json", "JSON", [".json"], "application/json", FileKind.Text, Lucide.Braces, FileGroup.Data);
    public static readonly FileType Yaml = new("yaml", "YAML", [".yaml", ".yml"], "application/yaml", FileKind.Text, Lucide.FileCog, FileGroup.Data);
    public static readonly FileType Toml = new("toml", "TOML", [".toml"], "application/toml", FileKind.Text, Lucide.FileCog, FileGroup.Data);
    public static readonly FileType Xml = new("xml", "XML", [".xml"], "application/xml", FileKind.Text, Lucide.FileCode, FileGroup.Data);

    /// <summary>A <c>data:</c> URL holding a whole file, for pasting into HTML or CSS. Written, never read.</summary>
    public static readonly FileType DataUri = new("datauri", "Data URI", [".txt"], "text/plain", FileKind.Text, Lucide.Link, FileGroup.Text, Abbreviation: "DATA URI");

    /// <summary>A zip of every icon a site needs, with the HTML that links them. Written, never read.</summary>
    public static readonly FileType Favicons = new("favicons", "Favicon set", [".zip"], "application/zip", FileKind.Binary, Lucide.AppWindow, FileGroup.Images, Abbreviation: "FAVICONS");

    public static readonly FileType Zip =new("zip", "ZIP archive", [".zip"], "application/zip", FileKind.Binary, Lucide.FileArchive, FileGroup.Data);
}
