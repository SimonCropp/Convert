/// <summary>Every Mermaid sample, one per diagram type offered, renders to every Mermaid target.</summary>
public class MermaidSampleTests
{
    static ConversionRegistry Registry => Fixtures.Registry;

    public static IEnumerable<(string Sample, string Conversion)> Cases() =>
        from sample in Registry.Samples
        where sample.Group == "Mermaid"
        from conversion in Registry.Conversions
        where conversion.Source == FileTypes.Mermaid
        orderby sample.FileName, conversion.Id
        select (sample.FileName, conversion.Id);

    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task Renders(string sample, string id)
    {
        var asset = Registry.Samples.Single(_ => _.FileName == sample).AssetUrl;
        var conversion = Registry.Conversions.Single(_ => _.Id == id);
        var source = new Document(sample, Fixtures.Asset(asset), FileTypes.Mermaid);
        var output = await conversion.RunAsync(
            source,
            conversion.CreateOptions(),
            new Progress<ConversionProgress>(),
            Cancel.None);

        await Assert.That(output.Parts.Count).IsEqualTo(1);
        await Assert.That(output.Parts[0].Bytes.Length).IsGreaterThan(0);
    }
}
