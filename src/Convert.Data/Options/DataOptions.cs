namespace Convert;

public sealed class DataOptions
{
    /// <summary>
    /// Read numbers, <c>true</c>/<c>false</c> and empty values as such from formats that hold only text
    /// (CSV, TSV, XML), instead of keeping every value a string.
    /// </summary>
    public bool InferTypes { get; set; } = true;

    /// <summary>Indented JSON, one value per line.</summary>
    public bool Indent { get; set; } = true;

    /// <summary>Set by the conversion: the source holds only text.</summary>
    public bool ShowInference { get; set; }

    /// <summary>Set by the conversion: the target is JSON.</summary>
    public bool ShowIndent { get; set; }

    /// <summary>Formats whose values are all text.</summary>
    internal static bool IsUntyped(FileType type) =>
        type == FileTypes.Csv || type == FileTypes.Tsv || type == FileTypes.Xml;
}
