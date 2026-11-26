/// <summary>
/// Whether a Markdown file has a Mermaid fence, for the routes that draw them with Naiad (Word and PDF):
/// they load Naiad and Skia, about 1.9 MB, only when it does. BCL only: it runs before any assembly loads.
/// </summary>
static class MermaidFences
{
    /// <summary>
    /// Whether any line has a fence marker and "mermaid": every Mermaid fence, in a list or a quote too,
    /// and the odd line that only mentions one. When it says no, Naiad is not loaded and the run does not
    /// draw, so a miss would leave a diagram as code rather than fail.
    /// </summary>
    public static bool Any(string markdown)
    {
        foreach (var line in markdown.AsSpan().EnumerateLines())
        {
            if ((line.Contains("```", StringComparison.Ordinal) || line.Contains("~~~", StringComparison.Ordinal)) &&
                line.Contains("mermaid", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
