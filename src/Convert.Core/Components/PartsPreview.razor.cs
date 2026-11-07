namespace Convert.Components;

/// <summary>A conversion result: one preview per part.</summary>
public partial class PartsPreview
{
    const int limit = 20;

    /// <summary>The result shown.</summary>
    [Parameter, EditorRequired]
    public ConversionOutput Output { get; set; } = default!;
}
