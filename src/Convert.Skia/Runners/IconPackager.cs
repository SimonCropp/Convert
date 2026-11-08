using System.IO.Compression;

namespace Convert;

/// <summary>
/// Icon files from square PNGs, BCL only: an ICO is a directory of PNG images (Vista onwards), and a
/// favicon set is a zip of the icons a site links, with its manifest and the HTML that links them.
/// </summary>
static class IconPackager
{
    /// <summary>Sizes in an ICO: the tab, the taskbar, the desktop, and Windows' large view.</summary>
    public static IReadOnlyList<int> IcoSizes { get; } = [16, 32, 48, 256];

    // The favicon.ico of a set: browsers that still ask for it want the small sizes only.
    static readonly int[] faviconSizes = [16, 32, 48];

    const int appleTouchSize = 180;

    static readonly int[] manifestSizes = [192, 512];

    /// <summary>Every size the favicon set needs.</summary>
    public static IReadOnlyList<int> FaviconSizes { get; } = [.. faviconSizes, appleTouchSize, .. manifestSizes];

    /// <summary>An ICO holding each PNG at its size.</summary>
    public static byte[] Ico(IReadOnlyList<(int Size, byte[] Png)> images)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        // ICONDIR: reserved, type 1 (icon), count.
        writer.Write((ushort) 0);
        writer.Write((ushort) 1);
        writer.Write((ushort) images.Count);
        var offset = 6 + 16 * images.Count;
        foreach (var (size, png) in images)
        {
            // ICONDIRENTRY: width and height (0 means 256), colours, reserved, planes, bits, size, offset.
            writer.Write((byte) (size >= 256 ? 0 : size));
            writer.Write((byte) (size >= 256 ? 0 : size));
            writer.Write((byte) 0);
            writer.Write((byte) 0);
            writer.Write((ushort) 1);
            writer.Write((ushort) 32);
            writer.Write(png.Length);
            writer.Write(offset);
            offset += png.Length;
        }

        foreach (var (_, png) in images)
        {
            writer.Write(png);
        }

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// The zip: favicon.ico, the source SVG (when there is one) as icon.svg, apple-touch-icon.png on an
    /// opaque background, the manifest's 192 and 512 PNGs, site.webmanifest, and favicon.html to paste.
    /// </summary>
    public static byte[] Favicons(Func<int, bool, byte[]> png, string? svg, string name)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(archive, "favicon.ico", Ico([.. faviconSizes.Select(_ => (_, png(_, false)))]));
            if (svg is not null)
            {
                Add(archive, "icon.svg", Encoding.UTF8.GetBytes(svg));
            }

            Add(archive, "apple-touch-icon.png", png(appleTouchSize, true));
            foreach (var size in manifestSizes)
            {
                Add(archive, $"icon-{size}.png", png(size, false));
            }

            Add(archive, "site.webmanifest", Encoding.UTF8.GetBytes(Manifest(name)));
            Add(archive, "favicon.html", Encoding.UTF8.GetBytes(Html(svg is not null)));
        }

        return stream.ToArray();
    }

    static void Add(ZipArchive archive, string name, byte[] bytes)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var output = entry.Open();
        output.Write(bytes);
    }

    static string Manifest(string name)
    {
        var icons = string.Join(
            ",\n",
            manifestSizes.Select(_ => $$"""    { "src": "/icon-{{_}}.png", "type": "image/png", "sizes": "{{_}}x{{_}}" }"""));
        return $$"""
                 {
                   "name": {{JsonString(name)}},
                   "icons": [
                 {{icons}}
                   ]
                 }

                 """;
    }

    static string JsonString(string text) =>
        System.Text.Json.Nodes.JsonValue.Create(text).ToJsonString();

    static string Html(bool hasSvg)
    {
        var builder = new StringBuilder();
        builder.Append("<!-- Copy the files to the site root and these lines into <head>. -->\n");
        builder.Append("<link rel=\"icon\" href=\"/favicon.ico\" sizes=\"32x32\">\n");
        if (hasSvg)
        {
            builder.Append("<link rel=\"icon\" href=\"/icon.svg\" type=\"image/svg+xml\">\n");
        }

        builder.Append("<link rel=\"apple-touch-icon\" href=\"/apple-touch-icon.png\">\n");
        builder.Append("<link rel=\"manifest\" href=\"/site.webmanifest\">\n");
        return builder.ToString();
    }
}
