namespace Convert;

/// <summary>Leaf values of the tree: typing text, and text for a value.</summary>
static class Values
{
    /// <summary>
    /// A value for <paramref name="text"/> read from a text-only format: with <paramref name="infer"/>,
    /// an integer, a decimal, <c>true</c>/<c>false</c> or (when empty) null; otherwise the text.
    /// </summary>
    public static JsonNode? Infer(string text, bool infer)
    {
        if (!infer)
        {
            return JsonValue.Create(text);
        }

        if (text.Length == 0)
        {
            return null;
        }

        if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create(true);
        }

        if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create(false);
        }

        if (IsNumber(text))
        {
            if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
            {
                return JsonValue.Create(integer);
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
            {
                return JsonValue.Create(number);
            }
        }

        return JsonValue.Create(text);
    }

    // JSON's number grammar: no leading zeros (so "007" and ZIP codes stay text), no leading "+" or ".".
    static bool IsNumber(string text)
    {
        var index = 0;
        if (text[0] == '-')
        {
            index++;
        }

        if (index >= text.Length || !char.IsAsciiDigit(text[index]))
        {
            return false;
        }

        if (text[index] == '0' && index + 1 < text.Length && char.IsAsciiDigit(text[index + 1]))
        {
            return false;
        }

        var digitsAfter = true;
        for (var i = index; i < text.Length; i++)
        {
            var current = text[i];
            if (char.IsAsciiDigit(current))
            {
                digitsAfter = true;
            }
            else if (current is '.' or 'e' or 'E' or '+' or '-')
            {
                digitsAfter = false;
            }
            else
            {
                return false;
            }
        }

        return digitsAfter;
    }

    /// <summary>A leaf as text: strings as they are, numbers and true/false as JSON writes them, null empty.</summary>
    public static string Text(JsonNode? node) =>
        node switch
        {
            null => "",
            JsonValue value when value.GetValueKind() == JsonValueKind.String => value.GetValue<string>(),
            _ => node.ToJsonString()
        };

    /// <summary>A key name made safe for <paramref name="used"/>: "name", then "name_2", "name_3".</summary>
    public static string Unique(string name, HashSet<string> used)
    {
        var candidate = name;
        for (var i = 2; !used.Add(candidate); i++)
        {
            candidate = $"{name}_{i}";
        }

        return candidate;
    }
}
