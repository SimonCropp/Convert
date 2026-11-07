namespace Convert.Previews;

/// <summary>Base of every preview component: it renders one <see cref="Convert.Document"/> read-only.</summary>
public abstract class PreviewBase : ComponentBase
{
    /// <summary>The document shown.</summary>
    [Parameter, EditorRequired]
    public Document Document { get; set; } = default!;
}

/// <summary>A preview that shows its document through a blob URL, revoked when the document changes.</summary>
public abstract class BlobPreviewBase : PreviewBase, IAsyncDisposable
{
    [Inject]
    BrowserInterop Interop { get; set; } = default!;

    Document? shown;

    protected string? Url { get; private set; }

    /// <summary>The MIME type the blob is created with.</summary>
    protected virtual string ContentType => Document.Type.ContentType;

    protected override async Task OnParametersSetAsync()
    {
        if (ReferenceEquals(shown, Document))
        {
            return;
        }

        shown = Document;
        await RevokeAsync();
        Url = await Interop.CreateObjectUrlAsync(ContentType, Document.Bytes);
    }

    async Task RevokeAsync()
    {
        if (Url is { } url)
        {
            Url = null;
            await Interop.RevokeObjectUrlAsync(url);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await RevokeAsync();
        }
        catch (JSDisconnectedException)
        {
        }

        GC.SuppressFinalize(this);
    }
}
