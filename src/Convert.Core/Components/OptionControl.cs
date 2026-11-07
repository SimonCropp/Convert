namespace Convert;

/// <summary>Base of the option controls, each bound with <c>@bind-Value</c>.</summary>
public abstract class OptionControl<TValue> : ComponentBase
{
    /// <summary>The visible label.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>An optional line under the control.</summary>
    [Parameter]
    public string? Hint { get; set; }

    /// <summary>The bound value.</summary>
    [Parameter]
    public TValue Value { get; set; } = default!;

    /// <summary>Raised with the new value.</summary>
    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    // Derived from the label rather than random, so component snapshots are stable.
    protected string Id =>
        "option-" + new string([.. Label.ToLowerInvariant().Select(_ => char.IsAsciiLetterOrDigit(_) ? _ : '-')]);

    protected Task SetAsync(TValue value)
    {
        Value = value;
        return ValueChanged.InvokeAsync(value);
    }
}
