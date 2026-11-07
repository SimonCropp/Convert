namespace Convert;

/// <summary>Whether a file's bytes are meaningful as UTF-8 text (and so can be pasted and edited as text).</summary>
public enum FileKind
{
    Binary,
    Text
}

/// <summary>The heading a target format is listed under in the "Convert to" picker.</summary>
public enum FileGroup
{
    Documents,
    Images,
    Data,
    Text
}

/// <summary>
/// A file format. Identity is <see cref="Id"/>: two packs that emit the same format use the same
/// <see cref="FileTypes"/> instance.
/// </summary>
/// <param name="Id">Stable lower-case id, e.g. "docx".</param>
/// <param name="DisplayName">Human name, e.g. "Word document".</param>
/// <param name="Extensions">File extensions including the dot; the first is canonical.</param>
/// <param name="ContentType">MIME type used for downloads and blob URLs.</param>
/// <param name="Kind">Text formats get the text editor and can be pasted.</param>
/// <param name="Icon">Lucide icon shown beside the format.</param>
/// <param name="Group">Heading in the target picker.</param>
/// <param name="PageNoun">What one rendered page is called ("slide" for a deck).</param>
public sealed record FileType(
    string Id,
    string DisplayName,
    IReadOnlyList<string> Extensions,
    string ContentType,
    FileKind Kind,
    Icon Icon,
    FileGroup Group,
    string PageNoun = "page")
{
    /// <summary>The canonical extension, including the dot.</summary>
    public string Extension => Extensions[0];

    /// <summary>Short label for chips, e.g. "DOCX".</summary>
    public string ShortName => Extension.TrimStart('.').ToUpperInvariant();

    public bool Equals(FileType? other) =>
        other is not null &&
        string.Equals(Id, other.Id, StringComparison.Ordinal);

    public override int GetHashCode() =>
        StringComparer.Ordinal.GetHashCode(Id);

    public bool Matches(string fileName) =>
        Extensions.Any(_ => fileName.EndsWith(_, StringComparison.OrdinalIgnoreCase));

    /// <summary>The <paramref name="fileName"/> with its extension replaced by this type's.</summary>
    public string FileNameFor(string fileName) =>
        Path.GetFileNameWithoutExtension(fileName) + Extension;
}
