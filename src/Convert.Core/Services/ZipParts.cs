/// <summary>Packs a multi-part result into one download.</summary>
public static class ZipParts
{
    /// <summary>
    /// The single part as it is, or every part in a zip named after <paramref name="sourceFileName"/>.
    /// </summary>
    public static Document Bundle(ConversionOutput output, string sourceFileName)
    {
        if (output.Parts.Count == 1)
        {
            return output.Parts[0];
        }

        return new(FileTypes.Zip.FileNameFor(sourceFileName), Zip(output.Parts), FileTypes.Zip);
    }

    public static byte[] Zip(IReadOnlyList<Document> parts)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in parts)
            {
                var name = part.FileName;
                for (var i = 2; !names.Add(name); i++)
                {
                    name = $"{Path.GetFileNameWithoutExtension(part.FileName)}_{i}{Path.GetExtension(part.FileName)}";
                }

                var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                using var stream = entry.Open();
                stream.Write(part.Bytes);
            }
        }

        return memory.ToArray();
    }

    /// <summary>The <c>page_0001.png</c> name for page <paramref name="index"/> (zero-based).</summary>
    public static string PageName(int index, FileType type) =>
        $"page_{index + 1:D4}{type.Extension}";
}
