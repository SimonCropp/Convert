namespace Convert;

/// <summary>CSV or TSV as a table: the header row, then the first rows.</summary>
public partial class TablePreview
{
    const int limit = 500;

    List<string>? header;
    List<List<string>> rows = [];
    bool truncated;

    protected override void OnParametersSet()
    {
        var records = Delimited.Read(Document.Text, Delimited.For(Document.Type), limit + 2);
        header = records.Count > 0 ? records[0] : null;
        rows = [.. records.Skip(1).Take(limit)];
        truncated = records.Count > limit + 1;
    }
}
