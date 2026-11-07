namespace Convert.Web.Layout;

public partial class MainLayout : IDisposable
{
    [Inject]
    ThemePreferenceService ThemePreferenceService { get; set; } = default!;

    [Inject]
    IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    IToolLoader Loader { get; set; } = default!;

    [Inject]
    ConvertSession Session { get; set; } = default!;

    ThemeType currentTheme = ThemeType.Light;
    DownloadSize? downloadSize;
    long? liveBytes;
    long? peakBytes;
    PeriodicTimer? ramPoll;

    protected override async Task OnInitializedAsync()
    {
        Loader.Changed += OnLoaderChanged;
        currentTheme = await ThemePreferenceService.GetSavedThemeAsync();
        await JSRuntime.InvokeVoidAsync("themeManager.applyTheme", currentTheme.ToString());
    }

    void OnLoaderChanged() =>
        InvokeAsync(StateHasChanged);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        // The boot payload is sampled once, after the load event; RAM is polled since it moves with every file.
        downloadSize = await JSRuntime.InvokeAsync<DownloadSize>("appInfo.downloadSize");
        await SampleRamAsync();
        StateHasChanged();

        var poll = new PeriodicTimer(TimeSpan.FromSeconds(2));
        ramPoll = poll;
        _ = PollRamAsync(poll);
    }

    async Task PollRamAsync(PeriodicTimer timer)
    {
        try
        {
            while (await timer.WaitForNextTickAsync())
            {
                await InvokeAsync(async () =>
                {
                    var previous = (liveBytes, peakBytes);
                    await SampleRamAsync();
                    if ((liveBytes, peakBytes) != previous)
                    {
                        StateHasChanged();
                    }
                });
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    async Task SampleRamAsync()
    {
        liveBytes = GC.GetTotalMemory(false);
        var sample = await JSRuntime.InvokeAsync<long>("appInfo.ramBytes");
        if (sample > 0)
        {
            peakBytes = peakBytes is { } previousPeak ? Math.Max(previousPeak, sample) : sample;
        }
    }

    static string FormatMb(long bytes) =>
        $"{bytes / (1024d * 1024d):0.0} MB";

    readonly record struct DownloadSize(long Zipped, long Unzipped);

    async Task HandleThemeChanged(ThemeType newTheme)
    {
        currentTheme = newTheme;
        await ThemePreferenceService.SaveThemeAsync(newTheme);
        await JSRuntime.InvokeVoidAsync("themeManager.applyTheme", newTheme.ToString());
    }

    public void Dispose()
    {
        Loader.Changed -= OnLoaderChanged;
        ramPoll?.Dispose();
    }
}
