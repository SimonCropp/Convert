namespace Convert;

/// <summary>
/// Everything the packs contributed. Built once at startup from descriptors only: nothing here loads a
/// pack's heavy assemblies.
/// </summary>
public sealed class ConversionRegistry
{
    List<FileType> types = [];
    List<Conversion> conversions = [];
    List<Preview> previews = [];
    List<Editor> editors = [];
    List<Sniffer> sniffers = [];
    List<Sample> samples = [];
    List<IPack> packs = [];
    HashSet<string> conversionIds = new(StringComparer.Ordinal);
    IPack? current;

    /// <summary>Runs <paramref name="pack"/>'s registration, recording it as the owner of what it adds.</summary>
    public void Register(IPack pack)
    {
        current = pack;
        try
        {
            pack.Register(this);
        }
        finally
        {
            current = null;
        }

        packs.Add(pack);
    }

    /// <summary>
    /// Registers a readable type. A <see cref="FileKind.Text"/> type also gets the core text editor.
    /// Several packs may read one format: registering the same <see cref="FileTypes"/> instance again is a
    /// no-op; a different type claiming a registered id throws.
    /// </summary>
    public void Add(FileType type)
    {
        if (types.FirstOrDefault(_ => _.Id == type.Id) is { } existing)
        {
            if (ReferenceEquals(existing, type))
            {
                return;
            }

            throw new InvalidOperationException($"File type '{type.Id}' is already registered.");
        }

        types.Add(type);
        if (type.Kind == FileKind.Text)
        {
            editors.Add(new(type, [], typeof(Editors.TextEditor)));
        }
    }

    public void Add(Conversion conversion)
    {
        if (!conversionIds.Add(conversion.Id))
        {
            throw new InvalidOperationException($"Conversion '{conversion.Id}' is already registered.");
        }

        conversion.Pack = current;
        conversions.Add(conversion);
    }

    public void Add(Preview preview)
    {
        preview.Pack = current;
        previews.Add(preview);
    }

    /// <summary>Adds an editor. A pack's editor for a text type replaces the core text editor.</summary>
    public void Add(Editor editor)
    {
        editor.Pack = current;
        editors.RemoveAll(_ => _.Type == editor.Type && _.Component == typeof(Editors.TextEditor));
        editors.Add(editor);
    }

    public void Add(Sniffer sniffer) =>
        sniffers.Add(sniffer);

    public void Add(Sample sample)
    {
        if (samples.Any(_ => _.AssetUrl == sample.AssetUrl))
        {
            throw new InvalidOperationException($"Sample '{sample.AssetUrl}' is already registered.");
        }

        samples.Add(sample);
    }

    public IReadOnlyList<FileType> Types => types;

    public IReadOnlyList<Conversion> Conversions => conversions;

    public IReadOnlyList<Preview> Previews => previews;

    public IReadOnlyList<Editor> Editors => editors;

    public IReadOnlyList<Sample> Samples => samples;

    public IReadOnlyList<IPack> Packs => packs;

    /// <summary>The registered text types, for the "Paste text" picker.</summary>
    public IReadOnlyList<FileType> TextTypes => [.. types.Where(_ => _.Kind == FileKind.Text)];

    /// <summary>
    /// Identifies a file: by extension first (browser MIME types are empty for Office files), then by
    /// asking each sniffer, for extensions no type claims (<c>.json</c>).
    /// </summary>
    public FileType? Detect(string fileName, ReadOnlySpan<byte> bytes)
    {
        if (types.FirstOrDefault(_ => _.Matches(fileName)) is { } byExtension)
        {
            return byExtension;
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        foreach (var sniffer in sniffers)
        {
            if (sniffer(extension, bytes) is { } sniffed)
            {
                return sniffed;
            }
        }

        return null;
    }

    /// <summary>Every target reachable from <paramref name="source"/>, grouped for the picker.</summary>
    public IReadOnlyList<TargetGroup> TargetsFor(FileType source) =>
    [
        .. conversions
            .Where(_ => _.Source == source)
            .GroupBy(_ => _.Target)
            .Select(_ => new TargetOption(_.Key, [.. _.OrderBy(conversion => conversion.Priority).ThenBy(conversion => conversion.Id, StringComparer.Ordinal)]))
            .GroupBy(_ => _.Target.Group)
            .OrderBy(_ => _.Key)
            .Select(_ => new TargetGroup(_.Key, [.. _.OrderBy(option => option.Target.DisplayName, StringComparer.Ordinal)]))
    ];

    public IReadOnlyList<Preview> PreviewsFor(FileType type) =>
        [.. previews.Where(_ => _.Type == type).OrderBy(_ => _.Priority)];

    public IReadOnlyList<Editor> EditorsFor(FileType type) =>
        [.. editors.Where(_ => _.Type == type)];

    /// <summary>Every readable extension, for <c>&lt;InputFile accept&gt;</c>.</summary>
    public string Accept =>
        string.Join(',', types.SelectMany(_ => _.Extensions).Append(".json").Distinct(StringComparer.OrdinalIgnoreCase));

    /// <summary>Every lazy assembly any entry declares.</summary>
    public IReadOnlyList<string> AllAssemblies =>
    [
        .. conversions.SelectMany(_ => _.Assemblies)
            .Concat(previews.SelectMany(_ => _.Assemblies))
            .Concat(editors.SelectMany(_ => _.Assemblies))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
    ];
}
