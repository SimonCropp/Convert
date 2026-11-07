namespace Convert.Components;

/// <summary>An indeterminate bar with a label.</summary>
public partial class ProgressBar
{
    /// <summary>What is happening, e.g. "Converting".</summary>
    [Parameter]
    public string Label { get; set; } = "Working";

    /// <summary>A secondary detail, e.g. the tool or the page.</summary>
    [Parameter]
    public string? Detail { get; set; }
}
