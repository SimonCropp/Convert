using System.Buffers.Binary;

/// <summary>
/// A Mermaid diagram as a PNG for another pack to embed (OfficeIMO's Markdown to Word): Naiad's SVG drawn
/// by Skia with the bundled Aptos, as <see cref="MermaidToPng"/> draws it. Names no Naiad or Skia type in
/// its signatures, so a pack's startup code can name <see cref="Assemblies"/>.
/// </summary>
public static class MermaidImage
{
    /// <summary>The lazy assemblies <see cref="Png"/> needs.</summary>
    public static IReadOnlyList<string> Assemblies => NaiadPack.SkiaAssemblies;

    /// <summary>
    /// The diagram at <paramref name="scale"/> times its own size, and that size in CSS pixels. Throws
    /// <see cref="UserInputException"/> when Naiad cannot parse or lay it out.
    /// </summary>
    public static (byte[] Png, double Width, double Height) Png(string mermaid, SkiaFonts.Faces fonts, double scale)
    {
        var options = new MermaidOptions();
        var svg = MermaidRunner.PlainSvg(mermaid, options);
        var raster = new RasterOptions
        {
            Scale = scale,
            Background = options.Background,
            SupportsAlpha = true
        };
        var png = SvgRunner.Raster(svg, FileTypes.Png, raster, fonts);
        // IHDR, right after the 8-byte signature and the chunk's length and type: width, then height.
        var width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4));
        return (png, width / scale, height / scale);
    }
}
