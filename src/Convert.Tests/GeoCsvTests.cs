/// <summary>
/// A CSV is a table unless its header names geometry columns: only then do GeoConvert's map preview
/// and geo targets apply.
/// </summary>
public class GeoCsvTests
{
    static ConversionRegistry Registry => Fixtures.Registry;

    [Test]
    public async Task PlainCsvIsATable()
    {
        var plain = Document.FromText("people.csv", "name,age\nAda,36\n", FileTypes.Csv);
        await Assert.That(Registry.PreviewsFor(plain)[0].Component.Name).IsEqualTo("TablePreview");
        var targets = Targets(plain);
        await Assert.That(targets).Contains("xlsx");
        await Assert.That(targets).DoesNotContain("geojson");
        await Assert.That(targets).DoesNotContain("png");
    }

    [Test]
    [Arguments("WKT,name\nPOINT (1 2),A\n")]
    [Arguments("name,Latitude,Longitude\nA,2,1\n")]
    public async Task GeoCsvIsAMap(string text)
    {
        var geo = Document.FromText("places.csv", text, FileTypes.Csv);
        await Assert.That(Registry.PreviewsFor(geo)[0].Component.Name).IsEqualTo("MapPreview");
        var targets = Targets(geo);
        await Assert.That(targets).Contains("geojson");
        await Assert.That(targets).Contains("xlsx");
    }

    static List<string> Targets(Document source) =>
        [.. Registry.TargetsFor(source).SelectMany(_ => _.Targets).Select(_ => _.Target.Id)];
}
