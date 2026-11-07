namespace Convert;

/// <summary>
/// The empty state: pick, drop or paste a file, or fetch a pack's sample. A drop anywhere on the page
/// is routed into the file input, so one <c>OnChange</c> path handles both.
/// </summary>
public partial class DropZone
{
    internal const long MaxFileBytes = 100L * 1024 * 1024;

    [Inject]
    BrowserInterop Interop { get; set; } = default!;

    [Inject]
    HttpClient Http { get; set; } = default!;

    /// <summary>The session a chosen file opens into.</summary>
    [Parameter, EditorRequired]
    public ConvertSession Session { get; set; } = default!;

    InputFile? input;
    bool pasting;
    string pasted = "";
    string pasteTypeId = FileTypes.Text.Id;
    Sample? loadingSample;
    string? openGroup;
    Exception? error;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && input?.Element is { } element)
        {
            try
            {
                await Interop.AcceptDropsAsync(element);
            }
            catch (JSException)
            {
            }
        }
    }

    async Task OnFileChosen(InputFileChangeEventArgs args)
    {
        error = null;
        try
        {
            var file = args.File;
            using var memory = new MemoryStream();
            await using (var stream = file.OpenReadStream(MaxFileBytes))
            {
                await stream.CopyToAsync(memory);
            }

            await Session.LoadAsync(file.Name, memory.ToArray());
        }
        catch (IOException exception)
        {
            error = new UserInputException($"The file could not be read: {exception.Message}", exception);
        }
    }

    void TogglePaste() => pasting = !pasting;

    Task UsePasted()
    {
        var type = Session.Registry.TextTypes.FirstOrDefault(_ => _.Id == pasteTypeId) ?? FileTypes.Text;
        return Session.LoadTextAsync(type, pasted);
    }

    /// <summary>The samples as buttons: each ungrouped one alone, each group where its first sample stands.</summary>
    List<(string? Group, List<Sample> Samples)> SampleEntries()
    {
        var entries = new List<(string? Group, List<Sample> Samples)>();
        foreach (var sample in Session.Registry.Samples)
        {
            var index = sample.Group is null ? -1 : entries.FindIndex(_ => _.Group == sample.Group);
            if (index < 0)
            {
                entries.Add((sample.Group, [sample]));
            }
            else
            {
                entries[index].Samples.Add(sample);
            }
        }

        return entries;
    }

    void ToggleGroup(string group) =>
        openGroup = openGroup == group ? null : group;

    void CloseGroup() => openGroup = null;

    void OnMenuKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            openGroup = null;
        }
    }

    async Task LoadSample(Sample sample)
    {
        openGroup = null;
        error = null;
        loadingSample = sample;
        try
        {
            var bytes = await Http.GetByteArrayAsync(sample.AssetUrl);
            await Session.LoadAsync(sample.FileName, bytes);
        }
        catch (HttpRequestException exception)
        {
            error = exception;
        }
        finally
        {
            loadingSample = null;
        }
    }
}
