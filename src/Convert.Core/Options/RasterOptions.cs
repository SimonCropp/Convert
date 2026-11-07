namespace Convert;

/// <summary>
/// Options every raster encoder shares (the browser's canvas, ImageSharp, Skia): size, quality and what
/// fills transparent pixels. The conversion sets <see cref="Lossy"/> and <see cref="SupportsAlpha"/> from
/// its target, which decide what the panel shows.
/// </summary>
public sealed class RasterOptions
{
    /// <summary>Output size relative to the source.</summary>
    public double Scale { get; set; } = 1;

    /// <summary>Fill behind transparent pixels, <c>#rrggbb</c>.</summary>
    public string Background { get; set; } = "#ffffff";

    /// <summary>Keep transparency (targets that have it only).</summary>
    public bool TransparentBackground { get; set; }

    /// <summary>Encoder quality 1–100 for lossy targets.</summary>
    public int Quality { get; set; } = 92;

    /// <summary>Set by the conversion: whether the target is lossy (shows <see cref="Quality"/>).</summary>
    public bool Lossy { get; set; }

    /// <summary>Set by the conversion: whether the target can keep transparency.</summary>
    public bool SupportsAlpha { get; set; }

    /// <summary>The background to flatten onto, or null to keep transparency.</summary>
    public string? Flatten => SupportsAlpha && TransparentBackground ? null : Background;

    /// <summary>Whether <paramref name="target"/> is encoded lossily (has a quality setting).</summary>
    public static bool IsLossy(FileType target) =>
        target == FileTypes.Jpeg || target == FileTypes.WebP;

    /// <summary>Whether <paramref name="target"/> can hold transparent pixels.</summary>
    public static bool HasAlpha(FileType target) =>
        target != FileTypes.Jpeg && target != FileTypes.Netpbm && target != FileTypes.Wbmp && target != FileTypes.Pdf;

    /// <summary>Defaults for <paramref name="target"/>: transparency kept wherever the target has it.</summary>
    public static RasterOptions For(FileType target) =>
        new()
        {
            Lossy = IsLossy(target),
            SupportsAlpha = HasAlpha(target),
            TransparentBackground = HasAlpha(target)
        };
}
