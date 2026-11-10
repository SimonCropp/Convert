/// <summary>
/// Carries a small source file in the URL fragment (<c>#name=…&amp;data=…</c>, the bytes deflated and
/// base64url-encoded) so a link reopens it. The fragment never reaches the server, so only the browser's
/// URL limit applies; <see cref="MaxLength"/> keeps links short enough to paste anywhere.
/// </summary>
public static class ShareLink
{
    /// <summary>The longest fragment written. Larger inputs get no link.</summary>
    public const int MaxLength = 8000;

    /// <summary>
    /// The fragment (without <c>#</c>) for <paramref name="document"/>, or null when it is too large. The
    /// file goes as it is when it fits; otherwise JSON and XML are tried compact, marked <c>&amp;min=1</c> so
    /// the link is laid out again when opened.
    /// </summary>
    public static string? Encode(Document document)
    {
        var fragment = Fragment(document.FileName, document.Bytes);
        if (fragment.Length <= MaxLength)
        {
            return fragment;
        }

        if (!PrettyPrint.Supports(document.Type))
        {
            return null;
        }

        var compact = PrettyPrint.Apply(document, false);
        if (compact.Bytes.Length >= document.Bytes.Length)
        {
            return null;
        }

        fragment = Fragment(document.FileName, compact.Bytes) + "&min=1";
        if (fragment.Length > MaxLength)
        {
            return null;
        }

        return fragment;
    }

    static string Fragment(string fileName, byte[] bytes)
    {
        using var buffer = new MemoryStream();
        using (var deflate = new DeflateStream(buffer, CompressionLevel.SmallestSize))
        {
            deflate.Write(bytes);
        }

        var data = System.Convert.ToBase64String(buffer.ToArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return $"name={Uri.EscapeDataString(fileName)}&data={data}";
    }

    /// <summary>
    /// Reads a fragment (with or without <c>#</c>) back into a file name and bytes. <paramref name="minified"/>
    /// says the bytes were written compact by <see cref="Encode"/> and should be laid out again.
    /// </summary>
    public static bool TryDecode(string? fragment, out string fileName, out byte[] bytes, out bool minified)
    {
        fileName = "";
        bytes = [];
        minified = false;
        if (string.IsNullOrEmpty(fragment))
        {
            return false;
        }

        string? name = null;
        string? data = null;
        foreach (var pair in fragment.TrimStart('#').Split('&'))
        {
            var split = pair.IndexOf('=');
            if (split < 0)
            {
                continue;
            }

            var value = pair[(split + 1)..];
            switch (pair[..split])
            {
                case "name":
                    name = Uri.UnescapeDataString(value);
                    break;
                case "data":
                    data = value;
                    break;
                case "min":
                    minified = value == "1";
                    break;
            }
        }

        if (string.IsNullOrEmpty(name) ||
            data is null)
        {
            return false;
        }

        try
        {
            var base64 = data.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            using var deflate = new DeflateStream(new MemoryStream(System.Convert.FromBase64String(base64)), CompressionMode.Decompress);
            using var output = new MemoryStream();
            deflate.CopyTo(output);
            fileName = name;
            bytes = output.ToArray();
            return true;
        }
        catch (Exception exception) when (exception is FormatException or InvalidDataException)
        {
            return false;
        }
    }
}
