namespace Convert;

public sealed class MarkdownHtmlOptions
{
    /// <summary>Markdig's advanced extension set (tables, footnotes, task lists, auto-links, …).</summary>
    public bool AdvancedExtensions { get; set; } = true;

    /// <summary>GitHub pipe tables, when the advanced set is off.</summary>
    public bool PipeTables { get; set; } = true;

    /// <summary>Treat a single line break as a <c>&lt;br&gt;</c>.</summary>
    public bool SoftLineBreakAsHard { get; set; }

    /// <summary>Turn <c>:smile:</c> shortcodes into emoji.</summary>
    public bool Emoji { get; set; }

    /// <summary>Wrap the output in a full HTML document with a stylesheet.</summary>
    public bool FullDocument { get; set; } = true;

    /// <summary>Indent the HTML, one block per line.</summary>
    public bool PrettyPrint { get; set; } = true;
}
