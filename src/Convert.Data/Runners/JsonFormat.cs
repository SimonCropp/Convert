static class JsonFormat
{
    static readonly JsonDocumentOptions reading = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    // Non-ASCII text stays readable rather than \u-escaped.
    static readonly JsonSerializerOptions compact = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static JsonNode? Read(string text)
    {
        try
        {
            return JsonNode.Parse(text, documentOptions: reading);
        }
        catch (JsonException exception)
        {
            throw new UserInputException($"The JSON could not be read: {exception.Message}", exception);
        }
    }

    // Laid out by Core's printer, as every pack's JSON is: lists of plain values stay on one line.
    public static string Write(JsonNode? tree, bool indent)
    {
        var json = tree is null ? "null" : tree.ToJsonString(compact);
        return indent ? PrettyPrint.Json(json) : json;
    }
}
