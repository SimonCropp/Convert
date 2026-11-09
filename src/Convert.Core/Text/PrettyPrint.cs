using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Convert;

/// <summary>
/// Lays out text output for reading: JSON, XML (and so SVG, KML, GPX) and HTML. BCL only, so any pack
/// can use it without a lazy assembly. Input that does not parse is returned unchanged: formatting never
/// fails a conversion.
/// </summary>
public static class PrettyPrint
{
    const string indentUnit = "  ";

    /// <summary>Whether <see cref="Apply(Document, bool)"/> has a layout for <paramref name="type"/>.</summary>
    public static bool Supports(FileType type) =>
        IsJson(type) || IsXml(type) || type == FileTypes.Html || type == FileTypes.Mermaid;

    static bool IsJson(FileType type) =>
        type == FileTypes.Json || type == FileTypes.GeoJson || type == FileTypes.TopoJson;

    static bool IsXml(FileType type) =>
        type == FileTypes.Xml || type == FileTypes.Svg || type == FileTypes.Kml || type == FileTypes.Gpx;

    /// <summary>
    /// Lays out <paramref name="document"/> by its type. Off (<paramref name="pretty"/> false), JSON and XML
    /// are written compact; HTML, Mermaid and other types are left as the tool wrote them.
    /// </summary>
    public static Document Apply(Document document, bool pretty)
    {
        var type = document.Type;
        var source = document.Text.TrimStart((char) 0xFEFF);
        string? text = null;
        if (IsJson(type))
        {
            text = Json(source, pretty);
        }
        else if (IsXml(type))
        {
            text = Xml(source, pretty);
        }
        else if (type == FileTypes.Html && pretty)
        {
            text = Html(source);
        }
        else if (type == FileTypes.Mermaid && pretty)
        {
            text = Mermaid(source);
        }

        return text is null ? document : document with { Bytes = Encoding.UTF8.GetBytes(text) };
    }

    /// <summary><see cref="Apply(Document, bool)"/> for every part.</summary>
    public static ConversionOutput Apply(ConversionOutput output, bool pretty) =>
        output with { Parts = [.. output.Parts.Select(_ => Apply(_, pretty))] };

    static readonly JsonSerializerOptions relaxed = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    static readonly JsonDocumentOptions lenient = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    /// <summary>
    /// JSON indented two spaces, or on one line when <paramref name="indent"/> is false. An array of only
    /// numbers, strings, booleans and nulls stays on one line, so coordinates read as <c>[151.2, -33.8]</c>.
    /// Numbers and strings keep their source spelling.
    /// </summary>
    public static string Json(string text, bool indent = true)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text, lenient);
        }
        catch (JsonException)
        {
            return text;
        }

        using (document)
        {
            var builder = new StringBuilder(text.Length);
            Value(builder, document.RootElement, indent, 0);
            if (indent)
            {
                builder.Append('\n');
            }

            return builder.ToString();
        }
    }

    static void Value(StringBuilder builder, JsonElement element, bool indent, int depth)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                Container(builder, element.EnumerateObject().ToList(), '{', '}', indent, depth, false, _ =>
                {
                    builder.Append(JsonSerializer.Serialize(_.Name, relaxed));
                    builder.Append(indent ? ": " : ":");
                    Value(builder, _.Value, indent, depth + 1);
                });
                break;
            case JsonValueKind.Array:
                var items = element.EnumerateArray().ToList();
                var flat = items.All(_ => _.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array));
                Container(builder, items, '[', ']', indent, depth, flat, _ => Value(builder, _, indent, depth + 1));
                break;
            case JsonValueKind.String:
                // Re-encoded, so escapes the source needed only for ASCII safety (é) read as the character.
                builder.Append(JsonSerializer.Serialize(element.GetString(), relaxed));
                break;
            default:
                builder.Append(element.GetRawText());
                break;
        }
    }

    static void Container<T>(StringBuilder builder, List<T> items, char open, char close, bool indent, int depth, bool flat, Action<T> write)
    {
        builder.Append(open);
        if (items.Count == 0)
        {
            builder.Append(close);
            return;
        }

        var multiline = indent && !flat;
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
                if (indent && flat)
                {
                    builder.Append(' ');
                }
            }

            if (multiline)
            {
                NewLine(builder, depth + 1);
            }

            write(items[i]);
        }

        if (multiline)
        {
            NewLine(builder, depth);
        }

        builder.Append(close);
    }

    /// <summary>
    /// XML with one element per line, or with the whitespace between elements removed when
    /// <paramref name="indent"/> is false. Elements holding text keep it exactly, on their own line.
    /// </summary>
    public static string Xml(string text, bool indent = true)
    {
        XDocument document;
        try
        {
            // Without PreserveWhitespace the whitespace-only text between elements is dropped (xml:space is
            // honoured), so the writer is free to lay the elements out again.
            document = XDocument.Parse(text);
        }
        catch (XmlException)
        {
            return text;
        }

        var writer = new Utf8StringWriter();
        var settings = new XmlWriterSettings
        {
            Indent = indent,
            IndentChars = indentUnit,
            NewLineChars = "\n",
            OmitXmlDeclaration = document.Declaration is null
        };
        using (var xml = XmlWriter.Create(writer, settings))
        {
            document.Save(xml);
        }

        var result = writer.ToString();
        return indent ? result + "\n" : result;
    }

    sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    // Block-level elements: whitespace beside these is not rendered, so they may start their own line.
    static readonly HashSet<string> blocks =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "html", "head", "body", "title", "meta", "link", "base", "style", "script", "noscript", "template",
        "header", "footer", "main", "nav", "section", "article", "aside", "address", "hgroup", "search",
        "div", "p", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "pre", "blockquote", "figure", "figcaption",
        "ul", "ol", "li", "dl", "dt", "dd", "menu", "details", "summary", "form", "fieldset", "legend",
        "table", "caption", "colgroup", "col", "thead", "tbody", "tfoot", "tr", "td", "th"
    ];

    static readonly HashSet<string> voids =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"
    ];

    // Copied verbatim to their closing tag: whitespace inside them is content.
    static readonly HashSet<string> verbatim =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "pre", "textarea", "script", "style"
    ];

    /// <summary>
    /// HTML with each block-level element on its own line, indented by nesting. Only the whitespace
    /// beside block-level tags changes, which browsers do not render; inline content, <c>&lt;pre&gt;</c>,
    /// <c>&lt;textarea&gt;</c>, <c>&lt;script&gt;</c> and <c>&lt;style&gt;</c> are kept as written.
    /// </summary>
    public static string Html(string text)
    {
        var builder = new StringBuilder(text.Length + text.Length / 8);
        // One entry per open block: its name, and whether a block started inside it (it then closes on its
        // own line). Names let a stray or omitted closing tag resynchronise rather than drift the indent.
        var open = new List<(string Name, bool HasBlocks)>();
        // Whether the current line holds inline content (text, inline tags) since the last block tag.
        var inline = false;
        var position = 0;

        void Break()
        {
            if (builder.Length > 0)
            {
                NewLine(builder, open.Count);
            }

            inline = false;
        }

        void MarkBlockChild()
        {
            if (open.Count > 0)
            {
                open[^1] = open[^1] with { HasBlocks = true };
            }
        }

        // Inline content goes on a new line when its container (or the top level) already holds blocks.
        bool AmongBlocks() =>
            open.Count == 0 ? builder.Length > 0 : open[^1].HasBlocks;

        while (position < text.Length)
        {
            var tagStart = text.IndexOf('<', position);
            var textEnd = tagStart < 0 ? text.Length : tagStart;
            if (textEnd > position)
            {
                var run = text.AsSpan(position, textEnd - position);
                if (!run.IsWhiteSpace())
                {
                    // Inline content after a block tag inside a container that holds blocks: its own line,
                    // without the whitespace that separated it from that tag.
                    if (!inline)
                    {
                        if (AmongBlocks())
                        {
                            Break();
                        }

                        run = run.TrimStart();
                    }

                    builder.Append(run);
                    inline = true;
                }
                else if (inline)
                {
                    // Whitespace between inline content is rendered: keep it, collapsed to what was written.
                    builder.Append(run);
                }
            }

            if (tagStart < 0)
            {
                break;
            }

            var tagEnd = TagEnd(text, tagStart);
            var tag = text.AsSpan(tagStart, tagEnd - tagStart);
            position = tagEnd;

            if (tag.StartsWith("<!--") || tag.StartsWith("<!") || tag.StartsWith("<?"))
            {
                if (!inline)
                {
                    Break();
                }

                builder.Append(tag);
                if (!tag.StartsWith("<!--"))
                {
                    inline = false;
                }

                continue;
            }

            var closing = tag.StartsWith("</");
            var name = TagName(tag[(closing ? 2 : 1)..]);
            if (!blocks.Contains(name))
            {
                if (!inline && AmongBlocks())
                {
                    Break();
                }

                builder.Append(tag);
                inline = true;
                continue;
            }

            if (closing)
            {
                var index = open.FindLastIndex(_ => string.Equals(_.Name, name, StringComparison.OrdinalIgnoreCase));
                var hadBlocks = false;
                if (index >= 0)
                {
                    hadBlocks = open[index].HasBlocks || index < open.Count - 1;
                    open.RemoveRange(index, open.Count - index);
                }

                if (hadBlocks)
                {
                    Break();
                }

                builder.Append(tag);
                inline = false;
                continue;
            }

            MarkBlockChild();
            Break();
            builder.Append(tag);
            if (verbatim.Contains(name))
            {
                var closeTag = "</" + name;
                var closeAt = text.IndexOf(closeTag, position, StringComparison.OrdinalIgnoreCase);
                var end = closeAt < 0 ? text.Length : TagEnd(text, closeAt);
                builder.Append(text.AsSpan(position, end - position));
                position = end;
                inline = false;
                continue;
            }

            if (!voids.Contains(name) && !tag.EndsWith("/>"))
            {
                open.Add((name, false));
            }
        }

        if (builder.Length > 0 && builder[^1] != '\n')
        {
            builder.Append('\n');
        }

        return builder.ToString();
    }

    const string mermaidIndent = "    ";

    // Diagrams whose indentation means nothing, so it can be laid out again. Others (mindmap, kanban,
    // treemap, block, …) read structure from indentation, and are left as written.
    static readonly HashSet<string> mermaidLayouts =
    [
        with(StringComparer.Ordinal),
        "graph", "flowchart", "flowchart-elk", "sequenceDiagram", "classDiagram", "classDiagram-v2",
        "stateDiagram", "stateDiagram-v2", "erDiagram", "gantt", "journey", "timeline", "gitGraph", "pie",
        "requirementDiagram", "quadrantChart", "xychart", "xychart-beta",
        "C4Context", "C4Container", "C4Component", "C4Dynamic", "C4Deployment"
    ];

    // Sequence diagram blocks, closed by `end`, and the lines that divide them.
    static readonly HashSet<string> sequenceBlocks =
        ["loop", "alt", "opt", "par", "par_over", "critical", "break", "rect", "box"];

    static readonly HashSet<string> sequenceDividers = ["else", "and", "option"];

    // Diagrams whose `section` lines head the lines after them.
    static readonly HashSet<string> sectioned = ["gantt", "journey", "timeline"];

    /// <summary>
    /// Mermaid with the diagram's body indented under its header and each block (<c>subgraph</c>,
    /// <c>loop</c>/<c>alt</c>, <c>{ … }</c>, <c>section</c>) indented within it; trailing whitespace and
    /// runs of blank lines removed. Front matter and multi-line strings are kept as written, and a diagram
    /// that reads structure from indentation (mindmap, kanban, …) is returned unchanged.
    /// </summary>
    public static string Mermaid(string text)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        var builder = new StringBuilder(text.Length + text.Length / 4);
        var index = 0;

        // Front matter (YAML between --- lines), as written.
        while (index < lines.Length && string.IsNullOrWhiteSpace(lines[index]))
        {
            index++;
        }

        if (index < lines.Length && lines[index].Trim() == "---")
        {
            builder.Append("---\n");
            index++;
            while (index < lines.Length)
            {
                var line = lines[index++];
                builder.Append(line.TrimEnd()).Append('\n');
                if (line.Trim() == "---")
                {
                    break;
                }
            }
        }

        // Directives and comments before the header stay at the margin.
        string? diagram = null;
        while (index < lines.Length)
        {
            var line = lines[index++].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            builder.Append(line).Append('\n');
            if (!line.StartsWith("%%"))
            {
                diagram = FirstWord(line);
                break;
            }
        }

        if (diagram is null || !mermaidLayouts.Contains(diagram))
        {
            return text;
        }

        var isSequence = diagram == "sequenceDiagram";
        var isFlowchart = diagram is "graph" or "flowchart" or "flowchart-elk";
        var isState = diagram.StartsWith("stateDiagram");
        var hasSections = sectioned.Contains(diagram);
        var depth = 1;
        var inSection = false;
        var blank = false;
        // Inside a quoted string left open at the end of a line (a multi-line label): copied as written.
        var inString = false;

        for (; index < lines.Length; index++)
        {
            var raw = lines[index];
            if (inString)
            {
                builder.Append(raw.TrimEnd()).Append('\n');
                inString = OpensString(raw, true);
                continue;
            }

            var line = raw.Trim();
            if (line.Length == 0)
            {
                blank = true;
                continue;
            }

            if (blank)
            {
                builder.Append('\n');
                blank = false;
            }

            var word = FirstWord(line);
            var lineDepth = depth;
            var comment = line.StartsWith("%%");
            if (!comment)
            {
                if (word == "end" || line.StartsWith('}'))
                {
                    depth = Math.Max(1, depth - 1);
                    lineDepth = depth;
                }
                else if ((isSequence && sequenceDividers.Contains(word)) ||
                         (isState && line == "--"))
                {
                    lineDepth = Math.Max(1, depth - 1);
                }
                else if (hasSections && word == "section")
                {
                    if (inSection)
                    {
                        depth--;
                    }

                    lineDepth = depth;
                    inSection = true;
                    depth++;
                }
            }

            for (var i = 0; i < lineDepth; i++)
            {
                builder.Append(mermaidIndent);
            }

            builder.Append(line).Append('\n');
            if (comment)
            {
                continue;
            }

            if (line.EndsWith('{') ||
                (isSequence && sequenceBlocks.Contains(word)) ||
                (isFlowchart && word == "subgraph"))
            {
                depth++;
            }

            inString = OpensString(line, false);
        }

        return builder.ToString();
    }

    // The diagram keyword or statement keyword: up to the first space, ';' or ':'.
    static string FirstWord(string line)
    {
        var end = line.AsSpan().IndexOfAny(' ', '\t', ';');
        var word = end < 0 ? line : line[..end];
        var colon = word.IndexOf(':');
        if (colon > 0)
        {
            return word[..colon];
        }

        return word;
    }

    // Whether a double-quoted string is still open at the end of the line, given whether it started open.
    static bool OpensString(string line, bool open)
    {
        foreach (var ch in line)
        {
            if (ch == '"')
            {
                open = !open;
            }
        }

        return open;
    }

    static string TagName(CharSpan afterBracket)
    {
        var length = 0;
        while (length < afterBracket.Length && (char.IsLetterOrDigit(afterBracket[length]) || afterBracket[length] is '-' or ':'))
        {
            length++;
        }

        return afterBracket[..length].ToString();
    }

    // The index after a tag's '>', skipping '>' inside quoted attribute values and comments.
    static int TagEnd(string text, int start)
    {
        if (string.CompareOrdinal(text, start, "<!--", 0, 4) == 0)
        {
            var close = text.IndexOf("-->", start + 4, StringComparison.Ordinal);
            return close < 0 ? text.Length : close + 3;
        }

        char? quote = null;
        for (var i = start + 1; i < text.Length; i++)
        {
            var ch = text[i];
            if (quote is not null)
            {
                if (ch == quote)
                {
                    quote = null;
                }
            }
            else if (ch is '"' or '\'')
            {
                quote = ch;
            }
            else if (ch == '>')
            {
                return i + 1;
            }
        }

        return text.Length;
    }

    static void NewLine(StringBuilder builder, int depth)
    {
        builder.Append('\n');
        for (var i = 0; i < depth; i++)
        {
            builder.Append(indentUnit);
        }
    }
}
