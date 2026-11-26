/// <summary>Markdown to Word draws Mermaid fences with Naiad, as pictures, instead of OfficeIMO's code panel.</summary>
public class MarkdownDocxTests
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

    [Test]
    public async Task MermaidFencesArePictures()
    {
        var conversion = Fixtures.Registry.Conversions.Single(_ => _.Id == "md-docx-officeimo");
        var output = await conversion.RunAsync(
            Document.FromText("diagrams.md", markdown, FileTypes.Markdown),
            conversion.CreateOptions(),
            new Progress<ConversionProgress>(),
            Cancel.None);

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

    // Naiad and Skia load only for a file with a fence to draw, in a list or a quote too.
    [Test]
    [Arguments("# Plain\n\nNo diagrams, though Mermaid is mentioned.\n\n```csharp\nvar x = 1;\n```", false)]
    [Arguments("```mermaid\nflowchart LR\n  A --> B\n```", true)]
    [Arguments("> - ~~~ Mermaid\n>   pie\n>   ~~~", true)]
    public async Task NaiadLoadsOnlyForDiagrams(string text, bool diagrams)
    {
        var conversion = Fixtures.Registry.Conversions.Single(_ => _.Id == "md-docx-officeimo");
        var assemblies = conversion.AssembliesFor(Document.FromText("file.md", text, FileTypes.Markdown));
        await Assert.That(assemblies.Contains("Naiad.wasm")).IsEqualTo(diagrams);
        await Assert.That(assemblies.Contains("SkiaSharp.wasm")).IsEqualTo(diagrams);
        await Assert.That(conversion.Assemblies.Except(assemblies).Any()).IsEqualTo(!diagrams);
        await Assert.That(assemblies.Except(conversion.Assemblies)).IsEmpty();
    }

    // The wizard asks the loader for the file's own list, and a file without diagrams converts with it.
    [Test]
    public async Task WizardLoadsWithoutNaiadForPlainMarkdown()
    {
        var loader = new StubToolLoader();
        using var session = new ConvertSession(Fixtures.Registry, loader);
        await session.LoadTextAsync(FileTypes.Markdown, "# Plain\n\n- one\n- two");
        session.SelectConversion(Fixtures.Registry.Conversions.Single(_ => _.Id == "md-docx-officeimo"));
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
