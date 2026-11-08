using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Convert;

/// <summary>
/// YAML through YamlDotNet's representation model (no reflection). Plain scalars resolve by the YAML 1.2
/// core schema; several documents in one file read as a list of them.
/// </summary>
static class YamlFormat
{
    const string stringTag = "tag:yaml.org,2002:str";

    public static JsonNode? Read(string text)
    {
        var stream = new YamlStream();
        try
        {
            stream.Load(new StringReader(text));
        }
        catch (YamlException exception)
        {
            throw new UserInputException($"The YAML could not be read: {exception.Message}", exception);
        }

        return stream.Documents.Count switch
        {
            0 => null,
            1 => Node(stream.Documents[0].RootNode),
            _ => new JsonArray([.. stream.Documents.Select(_ => Node(_.RootNode))])
        };
    }

    static JsonNode? Node(YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                var result = new JsonObject();
                foreach (var (key, value) in mapping.Children)
                {
                    result[key is YamlScalarNode scalar ? scalar.Value ?? "" : key.ToString()] = Node(value);
                }

                return result;
            case YamlSequenceNode sequence:
                return new JsonArray([.. sequence.Children.Select(Node)]);
            case YamlScalarNode scalar:
                return Scalar(scalar);
            default:
                return JsonValue.Create(node.ToString());
        }
    }

    static JsonNode? Scalar(YamlScalarNode scalar)
    {
        var text = scalar.Value ?? "";
        if (scalar.Style is not (ScalarStyle.Plain or ScalarStyle.Any) || scalar.Tag is { IsEmpty: false, IsNonSpecific: false, Value: stringTag })
        {
            return JsonValue.Create(text);
        }

        return Resolve(text);
    }

    // What a plain scalar means under the core schema: null, true/false, a number, or else the string.
    static JsonNode? Resolve(string text)
    {
        switch (text)
        {
            case "" or "~" or "null" or "Null" or "NULL":
                return null;
            case "true" or "True" or "TRUE":
                return JsonValue.Create(true);
            case "false" or "False" or "FALSE":
                return JsonValue.Create(false);
        }

        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
        {
            return JsonValue.Create(integer);
        }

        if (text.StartsWith("0x", StringComparison.Ordinal) &&
            long.TryParse(text.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var hex))
        {
            return JsonValue.Create(hex);
        }

        if (text.StartsWith("0o", StringComparison.Ordinal) && text.Length > 2 && text[2..].All(_ => _ is >= '0' and <= '7'))
        {
            return JsonValue.Create(System.Convert.ToInt64(text[2..], 8));
        }

        if (LooksDecimal(text) &&
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) &&
            double.IsFinite(number))
        {
            return JsonValue.Create(number);
        }

        return JsonValue.Create(text);
    }

    // Core schema float: digits with at most one point and an optional exponent (no "Infinity", no ",").
    static bool LooksDecimal(string text) =>
        text.Length > 0 &&
        text.Any(char.IsAsciiDigit) &&
        text.All(_ => char.IsAsciiDigit(_) || _ is '.' or 'e' or 'E' or '+' or '-');

    /// <summary>Whether a string written plain would read back as something else.</summary>
    static bool NeedsQuotes(string text) =>
        Resolve(text) is not JsonValue value || value.GetValueKind() != JsonValueKind.String;

    public static string Write(JsonNode? tree)
    {
        var writer = new StringWriter { NewLine = "\n" };
        var emitter = new Emitter(writer);
        emitter.Emit(new StreamStart());
        emitter.Emit(new DocumentStart());
        Emit(emitter, tree);
        emitter.Emit(new DocumentEnd(true));
        emitter.Emit(new StreamEnd());
        return writer.ToString();
    }

    static void Emit(IEmitter emitter, JsonNode? node)
    {
        switch (node)
        {
            case null:
                emitter.Emit(new Scalar("null"));
                break;
            case JsonObject value:
                var mappingStyle = value.Count == 0 ? MappingStyle.Flow : MappingStyle.Block;
                emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, true, mappingStyle));
                foreach (var (key, child) in value)
                {
                    EmitString(emitter, key);
                    Emit(emitter, child);
                }

                emitter.Emit(new MappingEnd());
                break;
            case JsonArray value:
                var sequenceStyle = value.Count == 0 ? SequenceStyle.Flow : SequenceStyle.Block;
                emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, true, sequenceStyle));
                foreach (var child in value)
                {
                    Emit(emitter, child);
                }

                emitter.Emit(new SequenceEnd());
                break;
            default:
                if (node.GetValueKind() == JsonValueKind.String)
                {
                    EmitString(emitter, node.GetValue<string>());
                }
                else
                {
                    emitter.Emit(new Scalar(node.ToJsonString()));
                }

                break;
        }
    }

    static void EmitString(IEmitter emitter, string text)
    {
        var style = NeedsQuotes(text)
            ? ScalarStyle.DoubleQuoted
            : text.Contains('\n')
                ? ScalarStyle.Literal
                : ScalarStyle.Any;
        emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, text, style, true, true));
    }
}
