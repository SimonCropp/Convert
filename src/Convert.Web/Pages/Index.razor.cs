namespace Convert.Web.Pages;

/// <summary>
/// The one page: Empty → Loaded → Converting → Converted, plus an Edit tab. All state is the
/// <see cref="ConvertSession"/>; this page lays it out and re-renders when it changes.
/// </summary>
public partial class Index
{
    [Inject]
    ConvertSession Session { get; set; } = default!;

    [Inject]
    BrowserInterop Interop { get; set; } = default!;

    View view = View.Source;
    Document? lastSource;

    enum View
    {
        Source,
        Edit,
        Result
    }

    TargetOption? TargetOption =>
        Session.Source is { } source && Session.Target is { } target
            ? Session.Registry.TargetsFor(source.Type).SelectMany(_ => _.Targets).FirstOrDefault(_ => _.Target == target)
            : null;

    protected override void OnInitialized() =>
        Session.Changed += OnSessionChanged;

    void OnSessionChanged()
    {
        // A new file (not an edit of this one) starts on its preview.
        if (Session.Source is null || (lastSource is not null && Session.Source.Type != lastSource.Type))
        {
            view = View.Source;
        }

        if (view == View.Result && Session.Result is null)
        {
            view = View.Source;
        }

        lastSource = Session.Source;
        InvokeAsync(StateHasChanged);
    }

    void Show(View next) => view = next;

    /// <summary>
    /// Text with a render of its own (Mermaid, Markdown, HTML, the text geo formats): edited beside its
    /// preview. Plain text, whose preview is the text again, is not.
    /// </summary>
    bool Splits(Document source) =>
        source.Type.Kind == FileKind.Text &&
        Session.Registry.EditorsFor(source.Type).Count > 0 &&
        Session.Registry.PreviewsFor(source.Type).Count > 0;

    void OnTargetSelected(FileType target)
    {
        Session.SelectTarget(target);
        if (view == View.Result)
        {
            view = Session.Result is null ? View.Source : View.Result;
        }
    }

    void OnConversionSelected(Conversion conversion) =>
        Session.SelectConversion(conversion);

    void OnOptionsChanged() =>
        Session.OptionsChanged();

    Task OnEdited(Document edited) =>
        Session.ReplaceSourceAsync(edited);

    async Task ConvertAsync()
    {
        await Session.ConvertAsync();
        if (Session.Result is not null)
        {
            view = View.Result;
        }
    }

    string DownloadName(ConversionOutput output) =>
        ZipParts.Bundle(output, Session.Source!.FileName).FileName;

    Task DownloadAsync(ConversionOutput output)
    {
        var file = ZipParts.Bundle(output, Session.Source!.FileName);
        return Interop.DownloadAsync(file.FileName, file.Type.ContentType, file.Bytes);
    }

    void StartOver()
    {
        view = View.Source;
        Session.Reset();
    }

    public void Dispose() =>
        Session.Changed -= OnSessionChanged;
}
