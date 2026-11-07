namespace Convert;

/// <summary>The target formats reachable from the source, as chips grouped by <see cref="FileGroup"/>.</summary>
public partial class TargetPicker
{
    [Inject]
    ConversionRegistry Registry { get; set; } = default!;

    /// <summary>The source type.</summary>
    [Parameter, EditorRequired]
    public FileType Source { get; set; } = default!;

    /// <summary>The chosen target, highlighted.</summary>
    [Parameter]
    public FileType? Selected { get; set; }

    /// <summary>Raised with the target clicked.</summary>
    [Parameter]
    public EventCallback<FileType> OnSelected { get; set; }

    IReadOnlyList<TargetGroup> groups = [];

    protected override void OnParametersSet() =>
        groups = Registry.TargetsFor(Source);
}
