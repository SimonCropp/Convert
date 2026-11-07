namespace Convert.ImageSharp;

/// <summary>An image decoded and re-encoded by ImageSharp.</summary>
sealed class ImageSharpConversion(FileType source, FileType target) : Conversion<RasterOptions>
{
    public override string Id => $"{source.Id}-{target.Id}-imagesharp";
    public override FileType Source => source;
    public override FileType Target => target;
    public override Tool Tool => ImageSharpPack.Tool;

    public override string Summary => "Decoded and re-encoded in .NET; animation frames carry over where both formats have them.";

    public override string? Expect =>
        IsIcon(target)
            ? $"{target.DisplayName}s are at most 256×256: a larger image is scaled down to fit."
            : RasterOptions.HasAlpha(target)
                ? null
                : $"{target.DisplayName} has no transparency: transparent pixels take the background colour.";

    /// <summary>Icon formats: at most 256×256.</summary>
    internal static bool IsIcon(FileType type) =>
        type == FileTypes.Ico || type == FileTypes.Cur || type == FileTypes.Ani;

    public override IReadOnlyList<string> Assemblies => ImageSharpPack.Assemblies;
    public override Type OptionsComponent => typeof(Components.RasterOptionsPanel);

    // After the browser's canvas, which needs no download, wherever both serve a pair.
    public override int Priority => 10;

    public override object CreateOptions() => RasterOptions.For(target);

    protected override Task<ConversionOutput> RunAsync(Document document, RasterOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var bytes = ImageSharpRunner.Convert(document.Bytes, target, options, cancel);
        return Task.FromResult(new ConversionOutput(new Document(target.FileNameFor(document.FileName), bytes, target)));
    }
}
