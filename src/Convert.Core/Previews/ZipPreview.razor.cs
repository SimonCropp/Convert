namespace Convert;

/// <summary>The entries of a zip archive.</summary>
public partial class ZipPreview
{
    List<(string Name, long Size)> entries = [];
    string? error;

    protected override void OnParametersSet()
    {
        entries = [];
        error = null;
        try
        {
            using var stream = new MemoryStream(Document.Bytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            entries = [.. archive.Entries.Select(_ => (_.FullName, _.Length))];
        }
        catch (InvalidDataException exception)
        {
            error = exception.Message;
        }
    }

    internal static string FormatSize(long bytes) =>
        bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024d:0.#} KB",
            _ => $"{bytes / (1024d * 1024d):0.#} MB"
        };
}
