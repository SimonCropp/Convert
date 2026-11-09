namespace Convert;

/// <summary>
/// A plain textarea for every text type. Fires <see cref="Changed"/> once typing has paused for
/// <see cref="Debounce"/>, so the preview re-renders on idle rather than per keystroke.
/// </summary>
public partial class TextEditor : IDisposable
{
    internal static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);

    [Inject]
    ConversionRegistry Registry { get; set; } = default!;

    [Inject]
    IToolLoader Loader { get; set; } = default!;

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
    bool formatting;

    // A layout from PrettyPrint, or from a pack's formatter (Mermaid, by Naiad).
    bool CanFormat =>
        PrettyPrint.Supports(Document.Type) ||
        Registry.FormatterFor(Document.Type) is not null;

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
        var edited = Document with
        {
            Bytes = Encoding.UTF8.GetBytes(text)
        };
        shown = edited;
        await Changed.InvokeAsync(edited);
    }

    // Rewrites the text laid out by PrettyPrint, or by the type's pack formatter once its assemblies are
    // loaded. Text that does not parse is left as it is.
    async Task PrettyPrintAsync()
    {
        debounce?.Cancel();
        pending = false;
        var current = Document with { Bytes = Encoding.UTF8.GetBytes(text) };
        Document edited;
        if (Registry.FormatterFor(Document.Type) is { } formatter)
        {
            formatting = true;
            try
            {
                await Loader.EnsureAsync(formatter.Assemblies, formatter.Pack);
            }
            finally
            {
                formatting = false;
            }

            edited = current with { Bytes = Encoding.UTF8.GetBytes(formatter.Format(current.Text)) };
        }
        else
        {
            edited = PrettyPrint.Apply(current, true);
        }

        text = edited.Text;
        shown = edited;
        await Changed.InvokeAsync(edited);
    }

    public void Dispose()
    {
        debounce?.Cancel();
        debounce?.Dispose();
    }
}
