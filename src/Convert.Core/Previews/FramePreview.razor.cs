namespace Convert;

/// <summary>An HTML document in a sandboxed frame.</summary>
public partial class FramePreview
{
    /// <summary>HTML to show instead of the document's own text (a rendered Markdown preview, say).</summary>
    [Parameter]
    public string? Html { get; set; }
}
