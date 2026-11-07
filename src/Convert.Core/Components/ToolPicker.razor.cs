namespace Convert;

/// <summary>Shown when several tools serve one source/target pair: a radio list, best first.</summary>
public partial class ToolPicker
{
    /// <summary>The conversions offered, in priority order.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<Conversion> Conversions { get; set; } = [];

    /// <summary>The chosen conversion.</summary>
    [Parameter]
    public Conversion? Selected { get; set; }

    /// <summary>Raised with the conversion picked.</summary>
    [Parameter]
    public EventCallback<Conversion> OnSelected { get; set; }
}
