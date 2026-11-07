/// <summary>
/// Every conversion runs on desktop .NET against a real input (Krilla and ImageSharp have desktop
/// natives). The Browser pack's canvas conversions need a browser and run in <see cref="BrowserTests"/>;
/// Publisher has no sample file.
/// </summary>
public class ConversionTests
{
    static ConversionRegistry Registry => Fixtures.Registry;

    public static IEnumerable<string> Ids() =>
        Registry.Conversions
            .Where(_ => _.Tool.Id != "browser" && _.Source != FileTypes.Pub)
            .Select(_ => _.Id)
            .Order(StringComparer.Ordinal);

    [Test]
    [MethodDataSource(nameof(Ids))]
    public async Task Runs(string id)
    {
        var conversion = Registry.Conversions.Single(_ => _.Id == id);
        var source = await Inputs.ForAsync(conversion.Source);
        var output = await conversion.RunAsync(source, conversion.CreateOptions(), new Progress<ConversionProgress>(), Cancel.None);

        await Assert.That(output.Parts.Count).IsGreaterThan(0);
        foreach (var part in output.Parts)
        {
            await Assert.That(part.Bytes.Length).IsGreaterThan(0);
            await Assert.That(part.Type).IsEqualTo(conversion.Target);
        }

        if (conversion.Target == FileTypes.Png)
        {
            await Assert.That(output.Parts[0].Bytes[..4]).IsEquivalentTo(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        }

        if (conversion.Target == FileTypes.Pdf)
        {
            await Assert.That(Encoding.ASCII.GetString(output.Parts[0].Bytes, 0, 4)).IsEqualTo("%PDF");
        }
    }

    // Text results are deterministic enough to read: these are snapshotted.
    [Test]
    [Arguments("md-html-markdig")]
    [Arguments("md-html-officeimo")]
    [Arguments("mermaid-svg-naiad")]
    [Arguments("geojson-wkt-geoconvert")]
    [Arguments("docx-md-morph")]
    [Arguments("html-md-officeimo")]
    public async Task TextOutput(string id)
    {
        var conversion = Registry.Conversions.Single(_ => _.Id == id);
        var source = await Inputs.ForAsync(conversion.Source);
        var output = await conversion.RunAsync(source, conversion.CreateOptions(), new Progress<ConversionProgress>(), Cancel.None);
        await Verify(output.Parts[0].Text, conversion.Target == FileTypes.Wkt ? "txt" : conversion.Target.Extension.TrimStart('.'))
            .UseParameters(id);
    }

    [Test]
    public async Task MermaidParseErrorIsUserInput()
    {
        var conversion = Registry.Conversions.Single(_ => _.Id == "mermaid-svg-naiad");
        var source = Document.FromText("bad.mmd", "flowchart LR\n  A[unclosed -->", FileTypes.Mermaid);
        await Assert.That(async () => await conversion.RunAsync(source, conversion.CreateOptions(), new Progress<ConversionProgress>(), Cancel.None))
            .Throws<UserInputException>();
    }
}

/// <summary>A real input for each type: a published sample, or a conversion of one.</summary>
static class Inputs
{
    static readonly Dictionary<string, Task<Document>> cache = [];
    static readonly Lock gate = new();

    public static Task<Document> ForAsync(FileType type)
    {
        lock (gate)
        {
            if (!cache.TryGetValue(type.Id, out var task))
            {
                task = CreateAsync(type);
                cache[type.Id] = task;
            }

            return task;
        }
    }

    static async Task<Document> CreateAsync(FileType type)
    {
        switch (type.Id)
        {
            case "docx" or "xlsx" or "pptx":
                return Sample($"_content/Morph.Blazor/sample/sample{type.Extension}", type);
            case "md":
                return Sample("_content/Convert.Markdig/samples/readme.md", type);
            case "mermaid":
                return Sample("_content/Convert.Naiad/samples/flowchart.mmd", type);
            case "geojson":
                return Sample("_content/Convert.Geo/samples/capitals.geojson", type);
            case "html":
                return Document.FromText(
                    "page.html",
                    "<html><body><h1>Title</h1><p>Some <b>bold</b> and <i>italic</i> text.</p><ul><li>One</li><li>Two</li></ul><table><tr><th>A</th><th>B</th></tr><tr><td>1</td><td>2</td></tr></table></body></html>",
                    type);
            case "pdf":
                return Sample("_content/Convert.Browser/samples/sample.pdf", type);
            case "png":
                return await ConvertAsync("mermaid-png-naiad");
            case "svg":
                return await ConvertAsync("mermaid-svg-naiad");
            case "wbmp":
                return new("sample.wbmp", Wbmp(), type);
            default:
                if (type.Group == FileGroup.Images)
                {
                    return await ConvertAsync($"png-{type.Id}-imagesharp");
                }

                if (type.Group == FileGroup.Data)
                {
                    return await ConvertAsync($"geojson-{type.Id}-geoconvert");
                }

                throw new NotSupportedException($"No test input for {type.Id}.");
        }
    }

    // Nothing here encodes WBMP: an 8×2 one, by hand (type 0, fixed header 0, width, height, then 1-bit
    // rows, 1 = white): a white row over a row of alternating pixels.
    static byte[] Wbmp() => [0, 0, 8, 2, 0b1111_1111, 0b1010_1010];

    static Document Sample(string url, FileType type) =>
        new(Path.GetFileName(url), Fixtures.Asset(url), type);

    static async Task<Document> ConvertAsync(string id)
    {
        var conversion = Fixtures.Registry.Conversions.Single(_ => _.Id == id);
        var source = await ForAsync(conversion.Source);
        var output = await conversion.RunAsync(source, conversion.CreateOptions(), new Progress<ConversionProgress>(), Cancel.None);
        return output.Parts[0] with { FileName = conversion.Target.FileNameFor("sample") };
    }
}
