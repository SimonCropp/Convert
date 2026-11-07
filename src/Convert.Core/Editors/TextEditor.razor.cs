namespace Convert.Editors;

/// <summary>
/// A plain textarea for every text type. Fires <see cref="Changed"/> once typing has paused for
/// <see cref="Debounce"/>, so the preview re-renders on idle rather than per keystroke.
/// </summary>
public partial class TextEditor : IDisposable
{
    internal static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);

    /// <summary>The document edited.</summary>
    [Parameter, EditorRequired]
    public Document Document { get; set; } = default!;

    /// <summary>Raised with the edited document after typing pauses.</summary>
    [Parameter]
    public EventCallback<Document> Changed { get; set; }

    string text = "";
    Document? shown;
    CancelSource? debounce;
    bool pending;

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(shown, Document))
        {
            return;
        }

        shown = Document;
        if (!pending)
        {
            text = Document.Text;
        }
    }

    async Task OnInput(ChangeEventArgs args)
    {
        text = args.Value as string ?? "";
        debounce?.Cancel();
        debounce?.Dispose();
        var current = new CancelSource();
        debounce = current;
        pending = true;
        try
        {
            await Task.Delay(Debounce, current.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        pending = false;
        var edited = Document with { Bytes = Encoding.UTF8.GetBytes(text) };
        shown = edited;
        await Changed.InvokeAsync(edited);
    }

    public void Dispose()
    {
        debounce?.Cancel();
        debounce?.Dispose();
    }
}
