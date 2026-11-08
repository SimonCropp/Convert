namespace Convert;

/// <summary>The library behind a conversion, credited on the tool picker.</summary>
public sealed record Tool(string Id, string DisplayName, string Description, string ProjectUrl);

/// <summary>A file in memory: the source, one part of a result, or what a preview or editor shows.</summary>
public sealed record Document(string FileName, byte[] Bytes, FileType Type)
{
    /// <summary>The bytes as UTF-8 text, for <see cref="FileKind.Text"/> types.</summary>
    public string Text => Encoding.UTF8.GetString(Bytes);

    public static Document FromText(string fileName, string text, FileType type) =>
        new(fileName, Encoding.UTF8.GetBytes(text), type);
}

/// <summary>What a conversion produced. More than one part is downloaded as a zip.</summary>
public sealed record ConversionOutput(IReadOnlyList<Document> Parts, IReadOnlyList<string> Notes)
{
    public ConversionOutput(Document part, IReadOnlyList<string>? notes = null)
        : this([part], notes ?? [])
    {
    }
}

/// <summary>A step of a running conversion, for the progress bar.</summary>
public sealed record ConversionProgress(string Label, string? Detail = null);

/// <summary>Renders a <see cref="FileType"/> read-only. The component takes <c>[Parameter] Document Document</c>.</summary>
/// <param name="Type">The type shown.</param>
/// <param name="Assemblies">Lazy assemblies the component needs, the full transitive closure.</param>
/// <param name="Component">A component in the pack assembly.</param>
/// <param name="Priority">Lower wins when several previews serve a type.</param>
/// <param name="Accepts">
/// Narrows the preview to some documents of its type (a CSV with geometry columns), from the bytes
/// alone. BCL only: it runs before any assembly loads.
/// </param>
public sealed record Preview(FileType Type, IReadOnlyList<string> Assemblies, Type Component, int Priority = 0, Func<Document, bool>? Accepts = null)
{
    internal IPack? Pack { get; set; }
}

/// <summary>
/// Edits a <see cref="FileType"/>. The component takes <c>[Parameter] Document Document</c> and
/// <c>[Parameter] EventCallback&lt;Document&gt; Changed</c>.
/// </summary>
public sealed record Editor(FileType Type, IReadOnlyList<string> Assemblies, Type Component)
{
    internal IPack? Pack { get; set; }
}

/// <summary>A bundled example file offered on the empty screen, fetched only when chosen.</summary>
/// <param name="DisplayName">Button label, or the menu item's within a group.</param>
/// <param name="Icon">Button icon. A group's button takes its first sample's.</param>
/// <param name="AssetUrl">Path relative to the app base, e.g. <c>_content/Convert.Naiad/samples/flowchart.mmd</c>.</param>
/// <param name="FileName">The name the file is loaded under.</param>
/// <param name="Group">Samples sharing a group are one dropdown button labelled with it.</param>
public sealed record Sample(string DisplayName, Icon Icon, string AssetUrl, string FileName, string? Group = null);

/// <summary>One target format and every conversion that reaches it, best first.</summary>
public sealed record TargetOption(FileType Target, IReadOnlyList<Conversion> Conversions);

/// <summary>The targets under one <see cref="FileGroup"/> heading.</summary>
public sealed record TargetGroup(FileGroup Group, IReadOnlyList<TargetOption> Targets);

/// <summary>
/// Identifies a file whose extension is ambiguous (<c>.json</c>, <c>.csv</c>, <c>.txt</c>) from its bytes.
/// BCL only: sniffers run at startup-reachable code paths.
/// </summary>
public delegate FileType? Sniffer(string extension, ReadOnlySpan<byte> bytes);

/// <summary>A tool's contribution: its file types, conversions, previews, editors, sniffers and samples.</summary>
public interface IPack
{
    void Register(ConversionRegistry registry);

    /// <summary>Called once after the first successful load of any of this pack's entries.</summary>
    void Loaded(IServiceProvider services)
    {
    }
}
