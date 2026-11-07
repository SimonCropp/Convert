namespace Convert.Components;

/// <summary>
/// Shows a document with the best preview its type has, loading that preview's assemblies first.
/// </summary>
public partial class PreviewHost
{
    [Inject]
    ConversionRegistry Registry { get; set; } = default!;

    [Inject]
    IToolLoader Loader { get; set; } = default!;

    /// <summary>The document shown.</summary>
    [Parameter, EditorRequired]
    public Document Document { get; set; } = default!;

    Preview? preview;
    bool loading;
    Exception? error;

    Dictionary<string, object?> Parameters => new() { ["Document"] = Document };

    protected override async Task OnParametersSetAsync()
    {
        var candidates = Registry.PreviewsFor(Document.Type);
        var next = candidates.Count > 0 ? candidates[0] : null;
        if (next == preview && !loading)
        {
            return;
        }

        error = null;
        preview = null;
        if (next is null)
        {
            return;
        }

        loading = true;
        try
        {
            await Loader.EnsureAsync(next.Assemblies, next.Pack);
            preview = next;
        }
        catch (Exception exception)
        {
            error = exception;
        }
        finally
        {
            loading = false;
        }
    }
}
