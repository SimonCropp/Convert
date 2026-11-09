using SixLabors.ImageSharp;

/// <summary>The favicon set holds every file its HTML links, and the ICO every size.</summary>
public class IconTests
{
    static async Task<Document> RunAsync(string id)
    {
        var conversion = Fixtures.Registry.Conversions.Single(_ => _.Id == id);
        var source = await Inputs.ForAsync(conversion.Source);
        var output = await conversion.RunAsync(source, conversion.CreateOptions(), new Progress<ConversionProgress>(), Cancel.None);
        return output.Parts[0];
    }

    [Test]
    [Arguments("svg-favicons-skia", true)]
    [Arguments("png-favicons-skia", false)]
    public async Task FaviconSetHasEveryFile(string id, bool hasSvg)
    {
        var part = await RunAsync(id);
        using var archive = new ZipArchive(new MemoryStream(part.Bytes), ZipArchiveMode.Read);
        List<string> expected = ["favicon.ico", "apple-touch-icon.png", "icon-192.png", "icon-512.png", "site.webmanifest", "favicon.html"];
        if (hasSvg)
        {
            expected.Add("icon.svg");
        }

        await Assert.That(archive.Entries.Select(_ => _.FullName)).IsEquivalentTo(expected);
        foreach (var (name, size) in new[] {("apple-touch-icon.png", 180), ("icon-192.png", 192), ("icon-512.png", 512)})
        {
            await using var stream = await archive.GetEntry(name)!.OpenAsync();
            using var image = await Image.LoadAsync(stream);
            await Assert.That((image.Width, image.Height)).IsEqualTo((size, size));
        }
    }

    [Test]
    public async Task IcoHoldsEverySize()
    {
        // The ICONDIR header and its entries: width (0 is 256), then each image's length and offset, each a PNG.
        var bytes = (await RunAsync("svg-ico-skia")).Bytes;
        var count = BitConverter.ToUInt16(bytes, 4);
        var sizes = new List<int>();
        for (var i = 0; i < count; i++)
        {
            var entry = 6 + 16 * i;
            sizes.Add(bytes[entry] == 0 ? 256 : bytes[entry]);
            var offset = BitConverter.ToInt32(bytes, entry + 12);
            using var image = Image.Load(bytes.AsSpan(offset, BitConverter.ToInt32(bytes, entry + 8)));
            await Assert.That(image.Width).IsEqualTo(sizes[^1]);
        }

        await Assert.That(sizes).IsEquivalentTo([16, 32, 48, 256]);
    }
}
