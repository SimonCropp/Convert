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
        var types = new FileExtensionContentTypeProvider
        {
            Mappings = {[".wasm"] = "application/wasm"}
        };
        app.UseDefaultFiles(
            new DefaultFilesOptions
            {
                FileProvider = files
            });
        app.UseStaticFiles(
            new StaticFileOptions
            {
                FileProvider = files,
                ContentTypeProvider = types,
                ServeUnknownFileTypes = true
            });
        app.MapFallbackToFile(
            "index.html",
            new StaticFileOptions
            {
                FileProvider = files
            });
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
        await page.WaitForSelectorAsync(".drop-zone", new() {Timeout = 120000});
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
        await ChooseSampleAsync(page, "Mermaid/Flowchart");
        await page.WaitForSelectorAsync(".svg-preview img", new() {Timeout = 60000});
        await Assert.That(requested.Order()).IsEquivalentTo(["Naiad", "Parlot"]);
    }

    public static IEnumerable<Func<(string Sample, string Target, string? Tool, string Extension)>> Routes() =>
    [
        () => ("Markdown", "HTML", null, ".html"),
        () => ("Mermaid/Flowchart", "SVG", null, ".svg"),
        () => ("Map/GeoJSON", "KML", null, ".kml"),
        () => ("Map/GeoParquet", "GEOJSON", null, ".geojson"),
        () => ("MS Office/Word document", "PDF", "Morph", ".pdf"),
        () => ("MS Office/Word document", "PDF", "OfficeIMO", ".pdf"),
        () => ("MS Office/PowerPoint presentation", "PDF", "OfficeIMO", ".pdf"),
        () => ("MS Office/Excel workbook", "PDF", "OfficeIMO", ".pdf"),
        () => ("Markdown", "DOCX", "OfficeIMO", ".docx"),
        () => ("PDF", "DOCX", null, ".docx"),
        () => ("PDF", "TXT", null, ".txt"),
        () => ("PDF", "MD", null, ".md"),
        () => ("MS Office/Excel workbook", "CSV", null, ".csv"),
        () => ("Markdown", "PDF", null, ".pdf")
    ];

    public static IEnumerable<Func<(string Target, string Extension)>> PastedTextRoutes() =>
    [
        () => ("PDF", ".pdf"),
        () => ("DOCX", ".docx")
    ];

    [Test]
    [MethodDataSource(nameof(PastedTextRoutes))]
    public async Task PastedTextConverts(string target, string extension)
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await page.ClickAsync("button:has-text('Paste text')");
        await page.FillAsync(".paste-panel textarea", "Shopping list\n\n  - Café au lait\n  - 牛乳\n");
        await page.SelectOptionAsync("#paste-type", "txt");
        await page.ClickAsync("button:has-text('Use text')");
        await ConvertAndDownloadAsync(page, target, null, extension);
    }

    // A CSV without geometry previews as a table and offers XLSX, not the geo formats.
    [Test]
    public async Task PlainCsvPreviewsAsATableAndConvertsToXlsx()
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await page.SetInputFilesAsync(
            ".drop-zone input[type=file]",
            new FilePayload
            {
                Name = "people.csv",
                MimeType = "text/csv",
                Buffer = "name,age\nAda,36\n\"Smith, J\",41\n"u8.ToArray()
            });
        await page.WaitForSelectorAsync(".table-preview td:text-is('Smith, J')", new() {Timeout = 60000});
        await Assert.That(await page.Locator(".chip:text-is('GEOJSON')").CountAsync()).IsEqualTo(0);
        await ConvertAndDownloadAsync(page, "XLSX", null, ".xlsx");
    }

    // One route per pack, through the real lazy loads, the trimmed build and the download.
    [Test]
    [MethodDataSource(nameof(Routes))]
    public async Task SampleConvertsAndDownloads(string sample, string target, string? tool, string extension)
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await ChooseSampleAsync(page, sample);
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
                Buffer = """<svg xmlns="http://www.w3.org/2000/svg" width="40" height="30"><rect width="40" height="30" fill="#2b579a"/></svg>"""u8.ToArray()
            });
        await ConvertAndDownloadAsync(page, "PNG", null, ".png");
    }

    public static IEnumerable<Func<(string Source, string Target, string? Tool, string Extension)>> UploadRoutes() =>
    [
        // Skia: the native libSkiaSharp linked into dotnet.native.wasm, its decoders, encoders and PDF
        // backend, and Svg.Skia with the bundled font.
        () => ("svg", "PDF", null, ".pdf"),
        () => ("svg", "PNG", "Skia", ".png"),
        () => ("gif", "PNG", "Skia", ".png"),
        () => ("bmp", "JPG", "Skia", ".jpg"),
        () => ("ico", "WEBP", "Skia", ".webp"),
        () => ("wbmp", "PNG", null, ".png"),
        () => ("png", "PDF", null, ".pdf"),
        // ImageSharp: formats only it writes, and sources only it reads (previewed through it too).
        () => ("png", "TIFF", null, ".tiff"),
        () => ("tga", "PNG", "ImageSharp", ".png"),
        () => ("exr", "JPG", "ImageSharp", ".jpg"),
        () => ("pnm", "QOI", null, ".qoi")
    ];

    // A file of each new image format (made on desktop by the same packs) uploaded and converted in the
    // published app, so the wasm build of each codec is what runs.
    [Test]
    [MethodDataSource(nameof(UploadRoutes))]
    public async Task UploadedImageConvertsAndDownloads(string source, string target, string? tool, string extension)
    {
        var input = await Inputs.ForAsync(Fixtures.Registry.Types.Single(_ => _.Id == source));
        var page = await browser!.NewPageAsync();
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await page.SetInputFilesAsync(
            ".drop-zone input[type=file]",
            new FilePayload
            {
                Name = input.FileName,
                MimeType = input.Type.ContentType,
                Buffer = input.Bytes
            });
        // The preview: the browser's own, or one decoded by ImageSharp or Skia.
        await page.WaitForSelectorAsync(".source-pane img", new() {Timeout = 60000});
        await ConvertAndDownloadAsync(page, target, tool, extension);
    }

    // Wide, Mermaid is edited beside its render, which follows the text once typing pauses.
    [Test]
    public async Task MermaidRenderFollowsTheEditor()
    {
        var page = await browser!.NewPageAsync();
        await page.SetViewportSizeAsync(1400, 900);
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await ChooseSampleAsync(page, "Mermaid/Flowchart");
        var image = page.Locator(".split-preview img");
        await image.WaitForAsync(new() {Timeout = 60000});
        var before = await image.GetAttributeAsync("src");

        await page.ClickAsync(".split-edit textarea");
        await page.Keyboard.PressAsync("Control+End");
        await page.Keyboard.TypeAsync("\n    C --> E[Celebrate]");
        await page.WaitForFunctionAsync(
            "previous => document.querySelector('.split-preview img')?.getAttribute('src') !== previous",
            before,
            new() {Timeout = 30000});

        // Still typing in the same editor, and the render has no error.
        await Assert.That(await page.EvaluateAsync<string>("document.activeElement.tagName")).IsEqualTo("TEXTAREA");
        await Assert.That(await page.Locator(".split-preview .error").CountAsync()).IsEqualTo(0);
    }

    // Text whose preview would be the text again opens in its editor: one panel, no Preview tab, and
    // tabs only once a result gives it a second view.
    [Test]
    public async Task JsonOpensInTheEditor()
    {
        var page = await browser!.NewPageAsync();
        await page.SetViewportSizeAsync(1400, 900);
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await ChooseSampleAsync(page, "Data/JSON");
        await page.WaitForSelectorAsync(".source-pane textarea", new() {Timeout = 60000});
        await Assert.That(await page.Locator(".split").CountAsync()).IsEqualTo(0);
        await Assert.That(await page.Locator(".text-preview").CountAsync()).IsEqualTo(0);
        await Assert.That(await page.Locator("[role=tab]:visible").CountAsync()).IsEqualTo(0);

        await ConvertAndDownloadAsync(page, "YAML", null, ".yaml");
        var tabs = await page.Locator("[role=tab]:visible").AllInnerTextsAsync();
        await Assert.That(tabs.Select(_ => _.Trim())).IsEquivalentTo(["Edit", "YAML Result"]);

        // Back on the source, it is still the editor.
        await page.ClickAsync("[role=tab]:text-is('Edit')");
        await page.WaitForSelectorAsync(".source-pane textarea", new() {Timeout = 60000});
    }

    // A lone tab is nothing to pick: a PDF (no editor) shows none until a result gives it a second.
    [Test]
    public async Task LoneTabIsHidden()
    {
        var page = await browser!.NewPageAsync();
        await page.SetViewportSizeAsync(1400, 900);
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await ChooseSampleAsync(page, "PDF");
        await page.WaitForSelectorAsync(".chip", new() {Timeout = 60000});
        await Assert.That(await page.Locator("[role=tab]:visible").CountAsync()).IsEqualTo(0);

        await ConvertAndDownloadAsync(page, "TXT", null, ".txt");
        var tabs = await page.Locator("[role=tab]:visible").AllInnerTextsAsync();
        await Assert.That(tabs.Select(_ => _.Trim())).IsEquivalentTo(["Preview", "TXT Result"]);
    }

    // Wide, split text shows "Edit and preview" as its only tab: hidden until there is a result.
    [Test]
    public async Task WideSplitHasNoLoneTab()
    {
        var page = await browser!.NewPageAsync();
        await page.SetViewportSizeAsync(1400, 900);
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await ChooseSampleAsync(page, "Mermaid/Flowchart");
        await page.WaitForSelectorAsync(".split-preview img", new() {Timeout = 60000});
        await Assert.That(await page.Locator("[role=tab]:visible").CountAsync()).IsEqualTo(0);
    }

    // Text typed in the Edit tab becomes the session's source: the next conversion has it.
    [Test]
    public async Task EditedDocxConvertsWithTheEdit()
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync(Url);
        await SettleAsync(page);
        await ChooseSampleAsync(page, "MS Office/Word document");
        await page.ClickAsync("[role=tab]:text-is('Edit')", new() {Timeout = 60000});
        await page.WaitForSelectorAsync(".editor-ribbon", new() {Timeout = 60000});
        var lines = page.Locator(".morph-preview .text-line");

        await lines.Filter(
                new()
                {
                    HasTextRegex = new("^PTA$")
                })
            .ClickAsync(
                new()
                {
                    Timeout = 60000
                });
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.TypeAsync(" EDITED");
        // Moving on from the paragraph commits what was typed.
        await lines.Filter(new() {HasText = "Mira Karlsson"}).First.ClickAsync();
        await page.WaitForSelectorAsync(".editor-dirty", new() {Timeout = 60000});

        var download = await ConvertAndDownloadAsync(page, "TXT", null, ".txt");
        var text = await File.ReadAllTextAsync(await download.PathAsync());
        // The heading is "pta" in all-caps style; text output drops the style.
        await Assert.That(text).Contains("pta EDITED");
    }

    // "Group/Item" opens the group's menu and picks the item; a plain name is a button of its own.
    static async Task ChooseSampleAsync(IPage page, string sample)
    {
        var slash = sample.IndexOf('/');
        if (slash < 0)
        {
            await page.ClickAsync($"button.sample:has-text('{sample}')");
            return;
        }

        await page.ClickAsync($"button.sample:has-text('{sample[..slash]}')");
        await page.ClickAsync($".sample-menu-item:text-is('{sample[(slash + 1)..]}')");
    }

    static async Task<IDownload> ConvertAndDownloadAsync(IPage page, string target, string? tool, string extension)
    {
        await page.ClickAsync($".chip:text-is('{target}')", new() {Timeout = 60000});
        if (tool is not null && await page.Locator(".tool-picker").CountAsync() > 0)
        {
            await page.ClickAsync($".tool-choice label:has-text('{tool}')");
        }

        await page.ClickAsync(".convert-button");
        // Either the result or an error: an error fails fast, with its text.
        await page.WaitForSelectorAsync(".download-button, .choices-pane .error", new() {Timeout = 240000});
        if (await page.Locator(".choices-pane .error").CountAsync() > 0)
        {
            throw new InvalidOperationException(await page.Locator(".choices-pane .error").InnerTextAsync());
        }

        var button = await page.QuerySelectorAsync(".download-button");
        var download = await page.RunAndWaitForDownloadAsync(() => button!.ClickAsync());
        await Assert.That(download.SuggestedFilename).EndsWith(extension);
        await Assert.That(await page.Locator(".error").CountAsync()).IsEqualTo(0);
        return download;
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
