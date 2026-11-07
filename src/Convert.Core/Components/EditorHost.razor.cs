namespace Convert.Components;

/// <summary>Edits a document with its type's editor, loading that editor's assemblies first.</summary>
public partial class EditorHost
{
    [Inject]
    ConversionRegistry Registry { get; set; } = default!;

    [Inject]
    IToolLoader Loader { get; set; } = default!;

    /// <summary>The document edited.</summary>
    [Parameter, EditorRequired]
    public Document Document { get; set; } = default!;

    /// <summary>Raised with the edited document.</summary>
    [Parameter]
    public EventCallback<Document> Changed { get; set; }

    Editor? editor;
    bool loading;
    Exception? error;

    Dictionary<string, object?> Parameters => new()
    {
        ["Document"] = Document,
        ["Changed"] = Changed
    };

    protected override async Task OnParametersSetAsync()
    {
        var candidates = Registry.EditorsFor(Document.Type);
        var next = candidates.Count > 0 ? candidates[0] : null;
        if (next == editor)
        {
            return;
        }

        error = null;
        editor = null;
        if (next is null)
        {
            return;
        }

        loading = true;
        try
        {
            await Loader.EnsureAsync(next.Assemblies, next.Pack);
            editor = next;
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
