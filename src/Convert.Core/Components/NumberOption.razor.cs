using System.Globalization;

namespace Convert;

/// <summary>A number input, clamped to <see cref="Min"/> and <see cref="Max"/>.</summary>
public partial class NumberOption<TValue>
{
    /// <summary>The smallest value accepted.</summary>
    [Parameter]
    public TValue? Min { get; set; }

    /// <summary>The largest value accepted.</summary>
    [Parameter]
    public TValue? Max { get; set; }

    /// <summary>The spinner's increment.</summary>
    [Parameter]
    public TValue? Step { get; set; }

    static string Format(TValue value) =>
        value.ToString(null, CultureInfo.InvariantCulture);

    static string? FormatOrNull(TValue? value) =>
        value is { } some ? Format(some) : null;

    Task OnChange(ChangeEventArgs args)
    {
        if (!TValue.TryParse(args.Value as string, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return Task.CompletedTask;
        }

        if (Min is { } min && parsed < min)
        {
            parsed = min;
        }

        if (Max is { } max && parsed > max)
        {
            parsed = max;
        }

        return SetAsync(parsed);
    }
}
