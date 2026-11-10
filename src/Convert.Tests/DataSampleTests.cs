/// <summary>
/// The Data samples: planets.json in every other data format, made by the Data pack. A missing one is
/// written into Convert.Data's wwwroot (and the test fails, so the next build publishes it).
/// </summary>
public class DataSampleTests
{
    static string SamplesDirectory => Path.Combine(ProjectFiles.SolutionDirectory, "Convert.Data", "wwwroot", "samples");

    [Test]
    public async Task EveryFormatHasASample()
    {
        var written = new List<string>();
        var source = new Document("planets.json", await File.ReadAllBytesAsync(Path.Combine(SamplesDirectory, "planets.json")), FileTypes.Json);
        var formats = Fixtures.Registry.Samples
            .Where(_ => _.Group == "Data")
            .Select(_ => Fixtures.Registry.Detect(_.FileName, [])!)
            .Where(_ => _ != FileTypes.Json);
        foreach (var format in formats)
        {
            var path = Path.Combine(SamplesDirectory, format.FileNameFor("planets"));
            if (File.Exists(path))
            {
                continue;
            }

            var conversion = Fixtures.Registry
                .Conversions
                .Single(_ => _.Source == FileTypes.Json && _.Target == format);
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
