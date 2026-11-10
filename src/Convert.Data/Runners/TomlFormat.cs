/// <summary>
/// TOML through Tomlyn's model types. TOML has no null and its root is always a table, so null values
/// are dropped and anything other than an object goes under a key.
/// </summary>
static class TomlFormat
{
    public static JsonNode? Read(string text)
    {
        TomlTable table;
        try
        {
            table = TomlSerializer.Deserialize<TomlTable>(text)!;
        }
        catch (TomlException exception)
        {
            throw new UserInputException($"The TOML could not be read: {exception.Message}", exception);
        }

        return Node(table);
    }

    static JsonNode? Node(object? value) =>
        value switch
        {
            null => null,
            TomlTable table => Object(table),
            TomlTableArray tables => new JsonArray([.. tables.Select(Node)]),
            TomlArray array => new JsonArray([.. array.Select(Node)]),
            string text => JsonValue.Create(text),
            bool flag => JsonValue.Create(flag),
            long integer => JsonValue.Create(integer),
            int integer => JsonValue.Create(integer),
            double number when double.IsFinite(number) => JsonValue.Create(number),
            float number when float.IsFinite(number) => JsonValue.Create(number),
            // Dates and times, infinity and NaN: their TOML text.
            _ => JsonValue.Create(System.Convert.ToString(value, CultureInfo.InvariantCulture))
        };

    static JsonObject Object(TomlTable table)
    {
        var result = new JsonObject();
        foreach (var (key, value) in table)
        {
            result[key] = Node(value);
        }

        return result;
    }

    public static string Write(JsonNode? tree)
    {
        var root = tree switch
        {
            JsonObject value => Table(value),
            null => new(),
            JsonArray => new()
            {
                ["items"] = Value(tree)!
            },
            _ => new()
            {
                ["value"] = Value(tree)!
            }
        };
        return TomlSerializer.Serialize(root);
    }

    static TomlTable Table(JsonObject value)
    {
        var table = new TomlTable();
        foreach (var (key, child) in value)
        {
            if (Value(child) is { } converted)
            {
                table[key] = converted;
            }
        }

        return table;
    }

    // Null for a null value, which TOML cannot hold: the caller leaves the key or item out.
    static object? Value(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return null;
            case JsonObject value:
                return Table(value);
            case JsonArray value:
                var items = value.Where(_ => _ is not null).ToList();
                if (items.Count > 0 && items.All(_ => _ is JsonObject))
                {
                    var tables = new TomlTableArray();
                    foreach (var item in items)
                    {
                        tables.Add(Table((JsonObject)item!));
                    }

                    return tables;
                }

                var array = new TomlArray();
                foreach (var item in items)
                {
                    array.Add(Value(item));
                }

                return array;
        }

        var leaf = node.AsValue();
        switch (leaf.GetValueKind())
        {
            case JsonValueKind.String:
                return leaf.GetValue<string>();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            default:
                if (leaf.TryGetValue<long>(out var integer))
                {
                    return integer;
                }

                return leaf.GetValue<double>();
        }
    }
}
