/// <summary>
/// Markdown to Word and to PDF draw Mermaid fences with Naiad, as pictures, instead of OfficeIMO's code
/// block, and load Naiad only for a file that has one.
/// </summary>
public class MarkdownMermaidTests
{
    const string markdown =
        """
        # Diagrams

        ```mermaid
        flowchart LR
          Start --> Stop
        ```

        - In a list:

          ```mermaid
          pie
            "Yes" : 3
            "No" : 1
          ```

        ```mermaid
        flowchart LR
          A[unclosed -->
        ```
        """;

    static Conversion Find(string id) =>
        Fixtures.Registry.Conversions.Single(_ => _.Id == id);

    static Task<ConversionOutput> RunAsync(string id, Document source)
    {
        var conversion = Find(id);
        return conversion.RunAsync(source, conversion.CreateOptions(), new Progress<ConversionProgress>(), Cancel.None);
    }

    static Task<ConversionOutput> RunAsync(string id) =>
        RunAsync(id, Document.FromText("diagrams.md", markdown, FileTypes.Markdown));

    [Test]
    public async Task WordPictures()
    {
        var output = await RunAsync("md-docx-officeimo");

        using var zip = new ZipArchive(new MemoryStream(output.Parts[0].Bytes));
        // OfficeIMO keeps pictures in /media, not Word's /word/media.
        var pictures = zip.Entries
            .Where(_ => _.FullName.Contains("media/", StringComparison.Ordinal))
            .ToList();
        var body = await new StreamReader(zip.GetEntry("word/document.xml")!.Open()).ReadToEndAsync();

        // The two diagrams Naiad draws are PNGs; the one it cannot parse stays code, with a note.
        await Assert.That(pictures.Count).IsEqualTo(2);
        await Assert.That(pictures.All(_ => _.FullName.EndsWith(".png", StringComparison.Ordinal))).IsTrue();
        await Assert.That(body).DoesNotContain("Start --&gt; Stop");
        await Assert.That(body).DoesNotContain("&quot;Yes&quot; : 3");
        await Assert.That(body).Contains("unclosed");
        await Assert.That(output.Notes.Count).IsEqualTo(1);
        await Assert.That(output.Notes[0]).StartsWith("Mermaid diagram 3 is kept as code");

        // A picture is set at the diagram's own size, half the PNG's (9525 EMU a pixel). The small
        // flowchart, which no page width caps.
        var smallest = pictures.Min(PngWidth) / 2d * 9525;
        var extent = Regex.Matches(body, """<wp:extent cx="(?<cx>\d+)" """)
            .Select(_ => long.Parse(_.Groups["cx"].Value))
            .Min();
        await Assert.That((double) extent).IsBetween(smallest - 9525, smallest + 9525);
    }

    [Test]
    public async Task PdfPictures()
    {
        var output = await RunAsync("md-pdf-officeimo");
        var pdf = output.Parts[0];
        // Image dictionaries are written uncompressed. Each picture is an RGB image whose transparency is a
        // second, soft-mask image.
        var pictures = Regex.Count(Encoding.Latin1.GetString(pdf.Bytes), @"/SMask\s+\d+\s+0\s+R");
        var text = (await RunAsync("pdf-txt-officeimo", pdf)).Parts[0].Text;

        // The two diagrams Naiad draws are images; the one it cannot parse stays code, with a note.
        await Assert.That(pictures).IsEqualTo(2);
        await Assert.That(text).DoesNotContain("Start");
        await Assert.That(text).DoesNotContain("Yes");
        await Assert.That(text).Contains("unclosed");
        await Assert.That(output.Notes.Count(_ => _.StartsWith("Mermaid diagram", StringComparison.Ordinal))).IsEqualTo(1);
        await Assert.That(output.Notes).Contains(_ => _.StartsWith("Mermaid diagram 3 is kept as code", StringComparison.Ordinal));
    }

    // Naiad and Skia load only for a file with a fence to draw, in a list or a quote too.
    [Test]
    [Arguments("md-docx-officeimo", "# Plain\n\nNo diagrams, though Mermaid is mentioned.\n\n```csharp\nvar x = 1;\n```", false)]
    [Arguments("md-docx-officeimo", "```mermaid\nflowchart LR\n  A --> B\n```", true)]
    [Arguments("md-docx-officeimo", "> - ~~~ Mermaid\n>   pie\n>   ~~~", true)]
    [Arguments("md-pdf-officeimo", "# Plain\n\nNo diagrams, though Mermaid is mentioned.\n\n```csharp\nvar x = 1;\n```", false)]
    [Arguments("md-pdf-officeimo", "```mermaid\nflowchart LR\n  A --> B\n```", true)]
    [Arguments("md-pdf-officeimo", "> - ~~~ Mermaid\n>   pie\n>   ~~~", true)]
    public async Task NaiadLoadsOnlyForDiagrams(string id, string text, bool diagrams)
    {
        var conversion = Find(id);
        var assemblies = conversion.AssembliesFor(Document.FromText("file.md", text, FileTypes.Markdown));
        await Assert.That(assemblies.Contains("Naiad.wasm")).IsEqualTo(diagrams);
        await Assert.That(assemblies.Contains("SkiaSharp.wasm")).IsEqualTo(diagrams);
        await Assert.That(conversion.Assemblies.Except(assemblies).Any()).IsEqualTo(!diagrams);
        await Assert.That(assemblies.Except(conversion.Assemblies)).IsEmpty();
    }

    // The wizard asks the loader for the file's own list, and a file without diagrams converts with it.
    [Test]
    [Arguments("md-docx-officeimo")]
    [Arguments("md-pdf-officeimo")]
    public async Task WizardLoadsWithoutNaiadForPlainMarkdown(string id)
    {
        var loader = new StubToolLoader();
        using var session = new ConvertSession(Fixtures.Registry, loader);
        await session.LoadTextAsync(FileTypes.Markdown, "# Plain\n\n- one\n- two");
        session.SelectConversion(Find(id));
        await session.ConvertAsync();

        await Assert.That(session.Error).IsNull();
        await Assert.That(session.Result!.Parts[0].Bytes.Length).IsGreaterThan(0);
        await Assert.That(loader.Requests.Single()).DoesNotContain("Naiad.wasm");
    }

    static long PngWidth(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        var header = new byte[24];
        stream.ReadExactly(header);
        return System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4));
    }
}
