namespace Convert;

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

    [Inject]
    NavigationManager Navigation { get; set; } = default!;

    View view = View.Source;
    Document? lastSource;
    string? sharedFragment;

    enum View
    {
        Source,
        Edit,
        Result
    }

    TargetOption? TargetOption
    {
        get
        {
            if (Session is
                {
                    Source: { } source,
                    Target: { } target
                })
            {
                return Session.Registry
                    .TargetsFor(source)
                    .SelectMany(_ => _.Targets)
                    .FirstOrDefault(_ => _.Target == target);
            }

            return null;
        }
    }

    protected override Task OnInitializedAsync()
    {
        Session.Changed += OnSessionChanged;
        Navigation.LocationChanged += OnLocationChanged;
        return OpenSharedAsync(Navigation.Uri);
    }

    // A link pasted into an open tab changes only the fragment: no reload, so open it here too.
    void OnLocationChanged(object? sender, LocationChangedEventArgs args) =>
        InvokeAsync(() => OpenSharedAsync(args.Location));

    /// <summary>A shared link carries its file in the fragment: open it, unless it is the one already open.</summary>
    Task OpenSharedAsync(string uri)
    {
        var fragment = new Uri(uri).Fragment.TrimStart('#');
        if (fragment == sharedFragment ||
            !ShareLink.TryDecode(fragment, out var fileName, out var bytes, out var minified))
        {
            return Task.CompletedTask;
        }

        sharedFragment = fragment;
        // Sent compact to fit the link: laid out again so it reads like a file.
        if (minified &&
            Session.Registry.Detect(fileName, bytes) is { } type)
        {
            bytes = PrettyPrint.Apply(new Document(fileName, bytes, type), true).Bytes;
        }

        return Session.LoadAsync(fileName, bytes);
    }

    /// <summary>
    /// Keeps the address bar a link to the current file while it is small enough to fit, so copying the
    /// URL shares it. A larger file, or none, clears the fragment.
    /// </summary>
    Task UpdateShareLinkAsync(Document? source)
    {
        string? fragment = null;
        if (source is not null &&
            source.Bytes.Length <= 256 * 1024)
        {
            fragment = ShareLink.Encode(source);
        }

        if (fragment == sharedFragment)
        {
            return Task.CompletedTask;
        }

        sharedFragment = fragment;
        return Interop.ReplaceHashAsync(fragment);
    }

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

        var sourceChanged = !ReferenceEquals(lastSource, Session.Source);
        lastSource = Session.Source;
        InvokeAsync(StateHasChanged);
        if (sourceChanged)
        {
            InvokeAsync(() => UpdateShareLinkAsync(Session.Source));
        }
    }

    void Show(View next) => view = next;

    /// <summary>
    /// Text with a render of its own (Mermaid, Markdown, HTML, a CSV's table, the text geo formats):
    /// edited beside its preview. Text whose preview is the text again is edited in place
    /// (<see cref="EditsInPlace"/>).
    /// </summary>
    bool Splits(Document source)
    {
        if (source.Type.Kind != FileKind.Text ||
            Session.Registry.EditorsFor(source.Type).Count == 0)
        {
            return false;
        }

        var previews = Session.Registry.PreviewsFor(source);
        return previews.Count > 0 && previews[0].Component != typeof(TextPreview);
    }

    /// <summary>
    /// Editable text whose preview would only be the text again (plain text, JSON, YAML, TOML, XML): the
    /// editor is the view.
    /// </summary>
    bool EditsInPlace(Document source)
    {
        if (source.Type.Kind != FileKind.Text ||
            Session.Registry.EditorsFor(source.Type).Count == 0)
        {
            return false;
        }

        var previews = Session.Registry.PreviewsFor(source);
        return previews.Count == 0 || previews[0].Component == typeof(TextPreview);
    }

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

    public void Dispose()
    {
        Session.Changed -= OnSessionChanged;
        Navigation.LocationChanged -= OnLocationChanged;
    }
}
