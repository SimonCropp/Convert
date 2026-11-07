namespace Convert;

/// <summary>The only place OpenXmlHtml's namespace is used.</summary>
static class OpenXmlHtmlRunner
{
    public static (byte[] Bytes, IReadOnlyList<string> Notes) ToDocx(string html, HtmlDocxOptions options)
    {
        var notes = new List<string>();
        // Images stay denied: the resolver does sync-over-async HTTP and File reads, both of which fail in
        // the browser.
        var settings = new HtmlConvertSettings
        {
            HeadingLevelOffset = options.HeadingLevelOffset,
            LocalImages = ImagePolicy.Deny(),
            WebImages = ImagePolicy.Deny(),
            OnDiagnostic = _ => notes.Add(Describe(_))
        };
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(html, stream, settings);
        return (stream.ToArray(), [.. notes.Distinct()]);
    }

    static string Describe(HtmlDiagnostic diagnostic) =>
        diagnostic.Kind switch
        {
            HtmlDiagnosticKind.UnsupportedElement => $"<{diagnostic.Name}> is not supported: {diagnostic.Reason}",
            HtmlDiagnosticKind.IgnoredAttribute => $"Attribute {diagnostic.Name}=\"{diagnostic.Value}\" ignored: {diagnostic.Reason}",
            _ => $"{diagnostic.Name}: {diagnostic.Value} dropped: {diagnostic.Reason}"
        };
}
