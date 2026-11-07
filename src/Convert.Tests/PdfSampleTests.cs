/// <summary>
/// The PDF sample: the Word sample through Morph. A missing one is written into Convert.Browser's wwwroot
/// (and the test fails, so the next build publishes it).
/// </summary>
public class PdfSampleTests
{
    static string SamplePath => Path.Combine(Fixtures.SrcDirectory, "Convert.Browser", "wwwroot", "samples", "sample.pdf");

    [Test]
    public async Task SampleExists()
    {
        if (File.Exists(SamplePath))
        {
            return;
        }

        var source = new Document(
            "sample.docx",
            Fixtures.Asset("_content/Morph.Blazor/sample/sample.docx"),
            FileTypes.Docx);
        var conversion = Fixtures.Registry.Conversions.Single(_ => _.Id == "docx-pdf-morph");
        var output = await conversion.RunAsync(
            source,
            conversion.CreateOptions(),
            new Progress<ConversionProgress>(),
            Cancel.None);
        Directory.CreateDirectory(Path.GetDirectoryName(SamplePath)!);
        await File.WriteAllBytesAsync(SamplePath, output.Parts[0].Bytes);
        Assert.Fail($"Wrote {SamplePath}; rebuild so it is published.");
    }
}
