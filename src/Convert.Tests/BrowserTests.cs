/// <summary>
/// The published, trimmed app in headless Chromium: boot loads no tool, one route per pack runs end to
/// end with a download, and the home page looks right.
/// </summary>
// Each test boots a WebAssembly runtime; one at a time so boots don't contend and time out.
[NotInParallel]
public class BrowserTests
{
    static WebApplication? app;
    static int port;
    static IPlaywright? playwright;
    static IBrowser? browser;
    static readonly HashSet<string> lazyNames = [.. Fixtures.Registry.AllAssemblies.Select(_ => _.Replace(".wasm", ""))];

    static string Url => $"http://localhost:{port}/";

    [Before(Class)]
    public static async Task StartAsync()
    {
        using (var listener = new TcpListener(IPAddress.Loopback, 0))
        {
            listener.Start();
            port = ((IPEndPoint) listener.LocalEndpoint).Port;
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://localhost:{port}");
        builder.Logging.ClearProviders();
        app = builder.Build();
        var files = new PhysicalFileProvider(Fixtures.Wwwroot);
        var types = new FileExtensionContentTypeProvider { Mappings = { [".wasm"] = "application/wasm" } };
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = files, ContentTypeProvider = types, ServeUnknownFileTypes = true });
        app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = files });
        await app.StartAsync();

        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync();
    }

    [After(Class)]
    public static async Task StopAsync()
    {
        if (browser is not null)
        {
            await browser.CloseAsync();
        }

        playwright?.Dispose();
        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [After(Test)]
    public async Task ClosePages()
    {
        foreach (var context in browser!.Contexts)
        {
            foreach (var page in context.Pages.ToList())
            {
                await page.CloseAsync();
            }
        }
    }

    static async Task SettleAsync(IPage page)
    {
        await page.WaitForSelectorAsync(".drop-zone", new() { Timeout = 120000 });
        await page.WaitForLoadStateAsync(LoadState.Load);
    }

    static string? LazyName(string url)
    {
        var file = url[(url.LastIndexOf('/') + 1)..];
        if (!file.EndsWith(".wasm", StringComparison.Ordinal))
        {
            return null;
        }

        // name.<fingerprint>.wasm
        var stem = file[..^".wasm".Length];
        var dot = stem.LastIndexOf('.');
        var name = dot > 0 ? stem[..dot] : stem;
        return lazyNames.Contains(name) ? name : null;
    }

    [Test]
    public async Task BootLoadsNoTool()
    {
        var page = await browser!.NewPageAsync();
        var requested = new List<string>();
        page.Request += (_, request) =>
        {
            if (LazyName(request.Url) is { } name)
            {
                requested.Add(name);
            }
        };
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await Assert.That(requested).IsEmpty();

        // Choosing a Mermaid sample loads Naiad, and only what its preview needs.
        await page.ClickAsync("button.sample:has-text('Mermaid flowchart')");
        await page.WaitForSelectorAsync(".svg-preview img", new() { Timeout = 60000 });
        await Assert.That(requested.Order()).IsEquivalentTo(["Naiad", "Parlot"]);
    }

    public static IEnumerable<Func<(string Sample, string Target, string? Tool, string Extension)>> Routes() =>
    [
        () => ("Markdown", "HTML", null, ".html"),
        () => ("Mermaid flowchart", "SVG", null, ".svg"),
        () => ("Map", "KML", null, ".kml"),
        () => ("DOCX sample", "PDF", "Morph", ".pdf"),
        () => ("DOCX sample", "PDF", "OfficeIMO", ".pdf"),
        () => ("Markdown", "DOCX", "OfficeIMO", ".docx")
    ];

    // One route per pack, through the real lazy loads, the trimmed build and the download.
    [Test]
    [MethodDataSource(nameof(Routes))]
    public async Task SampleConvertsAndDownloads(string sample, string target, string? tool, string extension)
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await page.ClickAsync($"button.sample:has-text('{sample}')");
        await ConvertAndDownloadAsync(page, target, tool, extension);
    }

    // OpenXmlHtml (html→docx) and the Browser pack's canvas (svg→png): sources with no sample.
    [Test]
    public async Task PastedHtmlConvertsToDocx()
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await page.ClickAsync("button:has-text('Paste text')");
        await page.FillAsync(".paste-panel textarea", "<h1>Hello</h1><p>From <b>HTML</b>.</p>");
        await page.SelectOptionAsync("#paste-type", "html");
        await page.ClickAsync("button:has-text('Use text')");
        await ConvertAndDownloadAsync(page, "DOCX", "OpenXmlHtml", ".docx");
    }

    [Test]
    public async Task UploadedSvgRastersToPng()
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await page.SetInputFilesAsync(
            ".drop-zone input[type=file]",
            new FilePayload
            {
                Name = "shape.svg",
                MimeType = "image/svg+xml",
                Buffer = Encoding.UTF8.GetBytes("""<svg xmlns="http://www.w3.org/2000/svg" width="40" height="30"><rect width="40" height="30" fill="#2b579a"/></svg>""")
            });
        await ConvertAndDownloadAsync(page, "PNG", null, ".png");
    }

    static async Task ConvertAndDownloadAsync(IPage page, string target, string? tool, string extension)
    {
        await page.ClickAsync($".chip:text-is('{target}')", new() { Timeout = 60000 });
        if (tool is not null && await page.Locator(".tool-picker").CountAsync() > 0)
        {
            await page.ClickAsync($".tool-choice label:has-text('{tool}')");
        }

        await page.ClickAsync(".convert-button");
        // Either the result or an error: an error fails fast, with its text.
        await page.WaitForSelectorAsync(".download-button, .choices-pane .error", new() { Timeout = 240000 });
        if (await page.Locator(".choices-pane .error").CountAsync() > 0)
        {
            throw new InvalidOperationException(await page.Locator(".choices-pane .error").InnerTextAsync());
        }

        var button = await page.QuerySelectorAsync(".download-button");
        var download = await page.RunAndWaitForDownloadAsync(() => button!.ClickAsync());
        await Assert.That(download.SuggestedFilename).EndsWith(extension);
        await Assert.That(await page.Locator(".error").CountAsync()).IsEqualTo(0);
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(true, false)]
    [Arguments(false, true)]
    public async Task HomePage(bool dark, bool mobile)
    {
        var page = await browser!.NewPageAsync();
        if (mobile)
        {
            await page.SetViewportSizeAsync(375, 667);
        }

        await page.GotoAsync(Url);
        if (dark)
        {
            await page.EvaluateAsync("() => localStorage.setItem('selectedTheme', 'Dark')");
            await page.ReloadAsync();
        }

        await SettleAsync(page);
        // The footer's figures vary per build and per run; pin them before the capture.
        await page.EvaluateAsync(
            """
            () => {
                const values = { '.footer-version': 'v0.0.0', '.footer-size': '0.0 MB zipped · 0.0 MB unzipped', '.footer-ram': '0.0 MB RAM (0.0 MB peak)' };
                for (const [selector, text] of Object.entries(values)) {
                    document.querySelectorAll(selector).forEach(_ => _.textContent = text);
                }
            }
            """);
        await Verify(page)
            .PageLoadState(null)
            .UseParameters(dark, mobile);
    }
}
