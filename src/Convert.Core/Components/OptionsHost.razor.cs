namespace Convert.Components;

/// <summary>Renders a conversion's options panel over its options POCO.</summary>
public partial class OptionsHost
{
    /// <summary>The conversion whose options are shown.</summary>
    [Parameter, EditorRequired]
    public Conversion Conversion { get; set; } = default!;

    /// <summary>The options POCO, from <see cref="Conversion.CreateOptions"/>.</summary>
    [Parameter, EditorRequired]
    public object Options { get; set; } = default!;

    /// <summary>Raised after any option changes.</summary>
    [Parameter]
    public EventCallback Changed { get; set; }

    Dictionary<string, object?> Parameters => new()
    {
        ["Options"] = Options,
        ["Changed"] = Changed
    };
}
