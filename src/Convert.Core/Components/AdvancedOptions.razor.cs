namespace Convert.Components;

/// <summary>A collapsed group of less-used options.</summary>
public partial class AdvancedOptions
{
    /// <summary>The expander's label.</summary>
    [Parameter]
    public string Title { get; set; } = "More options";

    /// <summary>The option controls inside.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
