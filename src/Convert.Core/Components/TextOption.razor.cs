namespace Convert;

/// <summary>A text input, or a colour picker (<c>#rrggbb</c>) when <see cref="Color"/> is set.</summary>
public partial class TextOption
{
    /// <summary>Render as <c>&lt;input type="color"&gt;</c>.</summary>
    [Parameter]
    public bool Color { get; set; }

    Task OnChange(ChangeEventArgs args) =>
        SetAsync(args.Value as string ?? "");
}
