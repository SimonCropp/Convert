namespace Convert;

/// <summary>A checkbox.</summary>
public partial class FlagOption
{
    Task OnChange(ChangeEventArgs args) =>
        SetAsync(args.Value is true);
}
