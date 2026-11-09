/// <summary>Core's text layout: readable, and never changing what the output means or renders as.</summary>
public class PrettyPrintTests
{
    [Test]
    public async Task JsonKeepsPlainListsOnOneLine()
    {
        var json = PrettyPrint.Json("""{"name":"Zürich","point":[8.54,47.37],"tags":[],"parts":[{"a":1},{"b":null}],"big":1.50E+3}""");
        await Assert.That(json).IsEqualTo(
            """
            {
              "name": "Zürich",
              "point": [8.54, 47.37],
              "tags": [],
              "parts": [
                {
                  "a": 1
                },
                {
                  "b": null
                }
              ],
              "big": 1.50E+3
            }

            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public async Task JsonCompact()
    {
        var json = PrettyPrint.Json("{\n  \"a\": [1, 2],\n  \"b\": { \"c\": \"\\u00e9\" }\n}", indent: false);
        await Assert.That(json).IsEqualTo("""{"a":[1,2],"b":{"c":"é"}}""");
    }

    [Test]
    public async Task InvalidInputIsUnchanged()
    {
        await Assert.That(PrettyPrint.Json("{not json")).IsEqualTo("{not json");
        await Assert.That(PrettyPrint.Xml("<a><b></a>")).IsEqualTo("<a><b></a>");
    }

    [Test]
    public async Task XmlIndentsElementsAndKeepsText()
    {
        var xml = PrettyPrint.Xml("""<?xml version="1.0" encoding="utf-8"?><svg xmlns="http://www.w3.org/2000/svg"><g><text x="1"> two  spaces </text><rect/></g></svg>""");
        await Assert.That(xml).IsEqualTo(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <svg xmlns="http://www.w3.org/2000/svg">
              <g>
                <text x="1"> two  spaces </text>
                <rect />
              </g>
            </svg>

            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public async Task XmlCompact()
    {
        var xml = PrettyPrint.Xml("<kml>\n  <Document>\n    <name>A</name>\n  </Document>\n</kml>\n", indent: false);
        await Assert.That(xml).IsEqualTo("<kml><Document><name>A</name></Document></kml>");
    }

    [Test]
    public async Task HtmlBreaksOnlyAtBlocks()
    {
        var html = PrettyPrint.Html(
            "<!doctype html><html><head><style>p {\n  margin: 0;\n}</style></head><body><h1>Title</h1>" +
            "<p>Some <b>bold</b> <i>and italic</i> text</p><ul><li>One</li><li>Two <a href=\"x\">link</a></li></ul>" +
            "<pre>  keep\n    this</pre><div>before<p>inside</p>after</div></body></html>");
        await Assert.That(html).IsEqualTo(
            """
            <!doctype html>
            <html>
              <head>
                <style>p {
              margin: 0;
            }</style>
              </head>
              <body>
                <h1>Title</h1>
                <p>Some <b>bold</b> <i>and italic</i> text</p>
                <ul>
                  <li>One</li>
                  <li>Two <a href="x">link</a></li>
                </ul>
                <pre>  keep
                this</pre>
                <div>before
                  <p>inside</p>
                  after
                </div>
              </body>
            </html>

            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public async Task HtmlSurvivesUnclosedAndStrayTags()
    {
        var html = PrettyPrint.Html("<div><p>one<p>two</div></span><p title=\"a > b\">three</p>");
        await Assert.That(html).IsEqualTo(
            """
            <div>
              <p>one
                <p>two
            </div>
            </span>
            <p title="a > b">three</p>

            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public async Task HtmlIsStable()
    {
        var once = PrettyPrint.Html("<html><body><table><tr><td>a</td><td><p>b</p></td></tr></table></body></html>");
        await Assert.That(PrettyPrint.Html(once)).IsEqualTo(once);
    }

    [Test]
    public async Task MermaidSequenceBlocks()
    {
        var mermaid = PrettyPrint.Mermaid(
            "sequenceDiagram\nAlice->>Bob: Hi   \n\n\n  loop Every minute\nalt ok\nBob-->>Alice: Fine\n        else not ok\nBob-->>Alice: Bad\nend\nend\n");
        await Assert.That(mermaid).IsEqualTo(
            """
            sequenceDiagram
                Alice->>Bob: Hi

                loop Every minute
                    alt ok
                        Bob-->>Alice: Fine
                    else not ok
                        Bob-->>Alice: Bad
                    end
                end

            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public async Task MermaidFlowchartKeepsFrontMatterAndStrings()
    {
        var mermaid = PrettyPrint.Mermaid(
            "---\ntitle: Demo\n---\n%%{init: {'theme':'forest'}}%%\nflowchart TD\nsubgraph one\nA[\"`two\n   lines`\"] --> B{Choice}\nend\nclassDef red fill:#f00\n");
        await Assert.That(mermaid).IsEqualTo(
            """
            ---
            title: Demo
            ---
            %%{init: {'theme':'forest'}}%%
            flowchart TD
                subgraph one
                    A["`two
               lines`"] --> B{Choice}
                end
                classDef red fill:#f00

            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public async Task MermaidBracesAndSections()
    {
        await Assert.That(PrettyPrint.Mermaid("classDiagram\nclass Animal {\n+name\n}\n")).IsEqualTo(
            "classDiagram\n    class Animal {\n        +name\n    }\n");
        await Assert.That(PrettyPrint.Mermaid("gantt\ntitle Plan\nsection A\nTask :a1, 2024-01-01, 3d\nsection B\nTask :b1, after a1, 2d\n")).IsEqualTo(
            "gantt\n    title Plan\n    section A\n        Task :a1, 2024-01-01, 3d\n    section B\n        Task :b1, after a1, 2d\n");
    }

    [Test]
    public async Task MermaidLeavesIndentationDiagramsAlone()
    {
        const string mindmap = "mindmap\n  root\n    child\n";
        await Assert.That(PrettyPrint.Mermaid(mindmap)).IsEqualTo(mindmap);
    }

    [Test]
    public async Task MermaidIsStable()
    {
        var once = PrettyPrint.Mermaid("stateDiagram-v2\nstate Active {\n[*] --> On\n--\n[*] --> Idle\n}\n");
        await Assert.That(PrettyPrint.Mermaid(once)).IsEqualTo(once);
    }
}
