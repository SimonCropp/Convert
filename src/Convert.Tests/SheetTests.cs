public class SheetTests
{
    // Through a workbook and back: text, numbers, a code with a leading zero and an id longer than
    // Excel's 15 digits all survive.
    [Test]
    public async Task CsvRoundTripsThroughXlsx()
    {
        const string csv = "id,name,amount,code\n007,\"Smith, J\",12.5,1234567890123456789\n8,Café,-3,\n";
        var registry = Fixtures.Registry;
        var toXlsx = registry.Conversions.Single(_ => _.Id == "csv-xlsx-officeimo");
        var xlsx = (await toXlsx.RunAsync(Document.FromText("people.csv", csv, FileTypes.Csv), toXlsx.CreateOptions(), new Progress<ConversionProgress>(), Cancel.None)).Parts[0];
        var toCsv = registry.Conversions.Single(_ => _.Id == "xlsx-csv-officeimo");
        var back = (await toCsv.RunAsync(xlsx, toCsv.CreateOptions(), new Progress<ConversionProgress>(), Cancel.None)).Parts[0];

        await Assert.That(back.Text.ReplaceLineEndings("\n")).IsEqualTo(csv);
    }
}
