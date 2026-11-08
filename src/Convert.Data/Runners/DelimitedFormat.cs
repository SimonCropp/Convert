namespace Convert;

/// <summary>
/// CSV and TSV to and from the tree: a list of objects keyed by the header row. Written, nested objects
/// flatten to dotted columns and lists are JSON text.
/// </summary>
static class DelimitedFormat
{
    public static JsonNode Read(string text, char delimiter, bool infer)
    {
        var records = Delimited.Read(text, delimiter);
        var rows = new JsonArray();
        if (records.Count == 0)
        {
            return rows;
        }

        var used = new HashSet<string>(StringComparer.Ordinal);
        var header = records[0].Select(_ => Values.Unique(_.Trim(), used)).ToList();
        foreach (var record in records.Skip(1))
        {
            if (record is [""])
            {
                continue;
            }

            var row = new JsonObject();
            for (var i = 0; i < Math.Max(header.Count, record.Count); i++)
            {
                var key = i < header.Count ? header[i] : Values.Unique($"column{i + 1}", used);
                if (i >= header.Count)
                {
                    header.Add(key);
                }

                row[key] = Values.Infer(i < record.Count ? record[i] : "", infer);
            }

            rows.Add(row);
        }

        return rows;
    }

    public static string Write(JsonNode? tree, char delimiter)
    {
        var rows = Rows(tree)
            .Select(_ =>
            {
                var cells = new Dictionary<string, string>(StringComparer.Ordinal);
                Flatten(_, "", cells);
                return cells;
            })
            .ToList();
        var columns = new List<string>();
        var known = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in rows.SelectMany(_ => _.Keys))
        {
            if (known.Add(key))
            {
                columns.Add(key);
            }
        }

        IEnumerable<IReadOnlyList<string>> records =
        [
            columns,
            .. rows.Select(_ => (IReadOnlyList<string>) [.. columns.Select(column => _.GetValueOrDefault(column, ""))])
        ];
        return Delimited.Write(records, delimiter);
    }

    // The rows a tree holds: a list; an object whose only value is a list (a TOML or XML wrapper); or
    // anything else as a single row.
    static IEnumerable<JsonNode?> Rows(JsonNode? tree)
    {
        while (tree is JsonObject { Count: 1 } single && single.First().Value is JsonObject or JsonArray)
        {
            tree = single.First().Value;
        }

        return tree switch
        {
            JsonArray array => array,
            _ => [tree]
        };
    }

    static void Flatten(JsonNode? node, string prefix, Dictionary<string, string> cells)
    {
        switch (node)
        {
            case JsonObject value:
                foreach (var (key, child) in value)
                {
                    Flatten(child, prefix.Length == 0 ? key : $"{prefix}.{key}", cells);
                }

                break;
            case JsonArray value:
                cells[Key(prefix)] = value.ToJsonString();
                break;
            default:
                cells[Key(prefix)] = Values.Text(node);
                break;
        }
    }

    static string Key(string prefix) =>
        prefix.Length == 0 ? "value" : prefix;
}
