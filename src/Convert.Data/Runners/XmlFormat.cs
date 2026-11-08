namespace Convert;

/// <summary>
/// XML to and from the tree, by the common convention: the root element is the one key of the top
/// object, attributes are <c>@name</c> keys, text beside child elements or attributes is <c>#text</c>, and
/// repeated child elements become a list (as does an element holding only <c>&lt;item&gt;</c> children).
/// </summary>
static class XmlFormat
{
    const string textKey = "#text";
    const string rootName = "root";
    const string itemName = "item";

    public static JsonNode? Read(string text, bool infer)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(text);
        }
        catch (XmlException exception)
        {
            throw new UserInputException($"The XML could not be read: {exception.Message}", exception);
        }

        var root = document.Root!;
        return new JsonObject
        {
            [root.Name.LocalName] = Element(root, infer)
        };
    }

    static JsonNode? Element(XElement element, bool infer)
    {
        var attributes = element.Attributes().Where(_ => !_.IsNamespaceDeclaration).ToList();
        var children = element.Elements().ToList();
        var text = string.Concat(element.Nodes().OfType<XText>().Select(_ => _.Value)).Trim();
        if (attributes.Count == 0 && children.Count == 0)
        {
            return Values.Infer(text, infer);
        }

        // Only <item> children, as written for a list: the list again.
        if (attributes.Count == 0 && text.Length == 0 && children.All(_ => _.Name.LocalName == itemName))
        {
            return new JsonArray([.. children.Select(_ => Element(_, infer))]);
        }

        var result = new JsonObject();
        foreach (var attribute in attributes)
        {
            result["@" + attribute.Name.LocalName] = Values.Infer(attribute.Value, infer);
        }

        foreach (var group in children.GroupBy(_ => _.Name.LocalName))
        {
            var items = group.Select(_ => Element(_, infer)).ToList();
            result[group.Key] = items.Count == 1 ? items[0] : new JsonArray([.. items]);
        }

        if (text.Length > 0)
        {
            result[textKey] = Values.Infer(text, infer);
        }

        return result;
    }

    public static string Write(JsonNode? tree, bool indent)
    {
        // A single key names the root element (a list under it becomes <item>s); anything else is wrapped.
        var root = tree is JsonObject { Count: 1 } single
            ? Element(single.First().Key, single.First().Value)
            : Element(rootName, tree);
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        var writer = new Utf8StringWriter();
        using (var xml = XmlWriter.Create(writer, new() { Indent = indent, IndentChars = "  ", NewLineChars = "\n" }))
        {
            document.Save(xml);
        }

        return indent ? writer + "\n" : writer.ToString();
    }

    static XElement Element(string name, JsonNode? node)
    {
        var element = new XElement(Name(name));
        switch (node)
        {
            case null:
                break;
            case JsonObject value:
                foreach (var (key, child) in value)
                {
                    if (key == textKey)
                    {
                        element.Add(new XText(Values.Text(child)));
                    }
                    else if (key.StartsWith('@') && key.Length > 1 && child is not (JsonObject or JsonArray))
                    {
                        element.SetAttributeValue(Name(key[1..]), Values.Text(child));
                    }
                    else if (child is JsonArray items)
                    {
                        element.Add(items.Select(_ => Element(key, _)));
                    }
                    else
                    {
                        element.Add(Element(key, child));
                    }
                }

                break;
            case JsonArray value:
                element.Add(value.Select(_ => Element(itemName, _)));
                break;
            default:
                element.Value = Values.Text(node);
                break;
        }

        return element;
    }

    // Keys with spaces or a leading digit are not XML names: encoded (_x0020_), as XmlConvert does.
    static XName Name(string key) =>
        XmlConvert.EncodeLocalName(key.Length == 0 ? "_" : key);

    sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
