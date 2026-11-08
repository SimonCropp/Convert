using System.Text.Encodings.Web;

namespace Convert;

static class JsonFormat
{
    static readonly JsonDocumentOptions reading = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    // Non-ASCII text stays readable rather than \u-escaped.
    static readonly JsonSerializerOptions indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

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

    public static string Write(JsonNode? tree, bool indent) =>
        tree is null ? "null" : tree.ToJsonString(indent ? indented : compact);
}
