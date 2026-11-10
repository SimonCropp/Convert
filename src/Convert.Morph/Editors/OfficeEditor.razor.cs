namespace Convert;

/// <summary>Edits a Word document or an Excel workbook in Morph's editor.</summary>
public partial class OfficeEditor
{
    /// <summary>The document edited.</summary>
    [Parameter, EditorRequired]
    public Document Document { get; set; } = default!;

    /// <summary>Raised with the edited document.</summary>
    [Parameter]
    public EventCallback<Document> Changed { get; set; }

    // The bytes the viewer opened. Not updated from our own edits coming back as Document: a new Source
    // reopens the file, which would drop the caret and the undo history on every keystroke.
    byte[]? source;
    byte[]? lastEdit;

    protected override void OnParametersSet()
    {
        if (source is null || !ReferenceEquals(Document.Bytes, lastEdit))
        {
            source = Document.Bytes;
        }
    }

    Task OnDocumentChanged(byte[] bytes)
    {
        lastEdit = bytes;
        return Changed.InvokeAsync(Document with { Bytes = bytes });
    }
}
