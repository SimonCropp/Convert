namespace Convert.Browser;

public sealed class CanvasOptions
{
    /// <summary>Output size relative to the source.</summary>
    public double Scale { get; set; } = 1;

    /// <summary>Fill behind transparent pixels, <c>#rrggbb</c>.</summary>
    public string Background { get; set; } = "#ffffff";

    /// <summary>Keep transparency (PNG and WebP only).</summary>
    public bool TransparentBackground { get; set; }

    /// <summary>Encoder quality 1–100 for JPEG and WebP.</summary>
    public int Quality { get; set; } = 92;

    /// <summary>Set by the conversion: whether the target is lossy (shows <see cref="Quality"/>).</summary>
    public bool Lossy { get; set; }

    /// <summary>Set by the conversion: whether the target can keep transparency.</summary>
    public bool SupportsAlpha { get; set; }
}
