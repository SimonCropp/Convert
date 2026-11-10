/// <summary>
/// The Image samples: Convert.Skia's sunset.svg drawn to PNG by Skia, and that PNG in every other
/// ImageSharp format. A missing one is written into Convert.ImageSharp's wwwroot (and the test fails,
/// so the next build publishes it).
/// </summary>
public class ImageSampleTests
{
    static string SvgPath => Path.Combine(Fixtures.SrcDirectory, "Convert.Skia", "wwwroot", "samples", "sunset.svg");

    static string SamplesDirectory => Path.Combine(Fixtures.SrcDirectory, "Convert.ImageSharp", "wwwroot", "samples");

    [Test]
    public async Task EveryFormatHasASample()
    {
        Directory.CreateDirectory(SamplesDirectory);
        var written = new List<string>();
        var svg = new Document("sunset.svg", await File.ReadAllBytesAsync(SvgPath), FileTypes.Svg);
        var png = await ConvertAsync(svg, "svg-png-skia");
        var pngPath = Path.Combine(SamplesDirectory, "sunset.png");
        if (!File.Exists(pngPath))
        {
            await File.WriteAllBytesAsync(pngPath, png.Bytes);
            written.Add("sunset.png");
        }

        var formats = Fixtures.Registry.Samples
            .Where(_ => _.Group == "Image")
            .Select(_ => Fixtures.Registry.Detect(_.FileName, [])!)
            .Where(_ => _ != FileTypes.Svg && _ != FileTypes.Png);
        foreach (var format in formats)
        {
            var path = Path.Combine(SamplesDirectory, format.FileNameFor("sunset"));
            if (File.Exists(path))
            {
                continue;
            }

            var output = await ConvertAsync(png, $"png-{format.Id}-imagesharp");
            await File.WriteAllBytesAsync(path, output.Bytes);
            written.Add(Path.GetFileName(path));
        }

        await Assert.That(written).IsEmpty();
    }

    static async Task<Document> ConvertAsync(Document source, string id)
    {
        var conversion = Fixtures.Registry.Conversions.Single(_ => _.Id == id);
        var output = await conversion.RunAsync(
            source,
            conversion.CreateOptions(),
            new Progress<ConversionProgress>(),
            Cancel.None);
        return output.Parts[0];
    }
}
