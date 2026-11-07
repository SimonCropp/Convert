namespace Convert;

/// <summary>Base of every conversion options panel. Panels bind controls to <see cref="Options"/>.</summary>
public abstract class OptionsPanel<TOptions> : ComponentBase
{
    /// <summary>The options POCO, mutated in place.</summary>
    [Parameter]
    public TOptions Options { get; set; } = default!;

    /// <summary>Raised after any option changes, so a cached result is dropped.</summary>
    [Parameter]
    public EventCallback Changed { get; set; }

    protected Task NotifyAsync() => Changed.InvokeAsync();
}
