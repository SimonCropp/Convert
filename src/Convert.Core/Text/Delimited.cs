namespace Convert;

/// <summary>
/// RFC 4180 delimited text (CSV, and TSV with a tab): quoted fields may hold the delimiter, quotes
/// (doubled) and line breaks. BCL only, shared by the data and spreadsheet routes and the table preview.
/// </summary>
public static class Delimited
{
    /// <summary>The delimiter a type uses: a tab for TSV, else a comma.</summary>
    public static char For(FileType type) =>
        type == FileTypes.Tsv ? '\t' : ',';

    /// <summary>Every record, each a list of fields. A trailing empty line is not a record.</summary>
    public static List<List<string>> Read(string text, char delimiter, int maxRecords = int.MaxValue)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var fieldStarted = false;
        var start = text.Length > 0 && text[0] == (char) 0xFEFF ? 1 : 0;
        for (var i = start; i < text.Length; i++)
        {
            var current = text[i];
            if (quoted)
            {
                if (current != '"')
                {
                    field.Append(current);
                }
                else if (i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    quoted = false;
                }

                continue;
            }

            if (current == '"' && field.Length == 0 && !fieldStarted)
            {
                quoted = true;
                fieldStarted = true;
            }
            else if (current == delimiter)
            {
                record.Add(field.ToString());
                field.Clear();
                fieldStarted = false;
            }
            else if (current is '\r' or '\n')
            {
                if (current == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                record.Add(field.ToString());
                field.Clear();
                fieldStarted = false;
                records.Add(record);
                record = [];
                if (records.Count >= maxRecords)
                {
                    return records;
                }
            }
            else
            {
                field.Append(current);
                fieldStarted = true;
            }
        }

        if (fieldStarted || field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            records.Add(record);
        }

        return records;
    }

    /// <summary>The records as text, quoting only the fields that need it, with CRLF line ends.</summary>
    public static string Write(IEnumerable<IReadOnlyList<string>> records, char delimiter)
    {
        var builder = new StringBuilder();
        foreach (var record in records)
        {
            for (var i = 0; i < record.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(delimiter);
                }

                AppendField(builder, record[i], delimiter);
            }

            builder.Append("\r\n");
        }

        return builder.ToString();
    }

    static void AppendField(StringBuilder builder, string field, char delimiter)
    {
        var needsQuotes = field.Length > 0 &&
                          (field.Contains(delimiter) ||
                           field.AsSpan().IndexOfAny('"', '\r', '\n') >= 0 ||
                           char.IsWhiteSpace(field[0]) ||
                           char.IsWhiteSpace(field[^1]));
        if (!needsQuotes)
        {
            builder.Append(field);
            return;
        }

        builder.Append('"');
        builder.Append(field.Replace("\"", "\"\""));
        builder.Append('"');
    }
}
