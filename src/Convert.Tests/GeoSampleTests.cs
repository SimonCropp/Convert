/// <summary>
/// The Map samples: capitals.geojson in every other geo format, made by GeoConvert. A missing one is
/// written into Convert.Geo's wwwroot (and the test fails, so the next build publishes it).
/// </summary>
public class GeoSampleTests
{
    static string SamplesDirectory => Path.Combine(Fixtures.SrcDirectory, "Convert.Geo", "wwwroot", "samples");

    [Test]
    public async Task EveryFormatHasASample()
    {
        var written = new List<string>();
        var source = new Document("capitals.geojson", await File.ReadAllBytesAsync(Path.Combine(SamplesDirectory, "capitals.geojson")), FileTypes.GeoJson);
        var formats = Fixtures.Registry.Samples
            .Where(_ => _.Group == "Map")
            .Select(_ => Fixtures.Registry.Detect(_.FileName, [])!)
            .Where(_ => _ != FileTypes.GeoJson);
        foreach (var format in formats)
        {
            var path = Path.Combine(SamplesDirectory, format.FileNameFor("capitals"));
            if (File.Exists(path))
            {
                continue;
            }

            var conversion = Fixtures.Registry
                .Conversions
                .Single(_ => _.Id == $"geojson-{format.Id}-geoconvert");
            var output = await conversion.RunAsync(
                source,
                conversion.CreateOptions(),
                new Progress<ConversionProgress>(),
                Cancel.None);
            await File.WriteAllBytesAsync(path, output.Parts[0].Bytes);
            written.Add(Path.GetFileName(path));
        }

        await Assert.That(written).IsEmpty();
    }
}
