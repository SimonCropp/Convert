namespace Convert.Components;

/// <summary>A drop-down over a fixed list of values.</summary>
public partial class ChoiceOption<TValue>
{
    /// <summary>The values offered, with their labels.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<(TValue Value, string Label)> Choices { get; set; } = [];

    Task OnChange(ChangeEventArgs args)
    {
        if (int.TryParse(args.Value as string, out var index) &&
            index >= 0 &&
            index < Choices.Count)
        {
            return SetAsync(Choices[index].Value);
        }

        return Task.CompletedTask;
    }
}
