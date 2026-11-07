using Convert.Components;
using Index = Convert.Web.Pages.Index;

/// <summary>Records which entries the wizard asked to load, and loads nothing (the real loader throws off-browser).</summary>
sealed class StubToolLoader : IToolLoader
{
    public List<IReadOnlyList<string>> Requests { get; } = [];

    public Task EnsureAsync(IReadOnlyList<string> assemblies, IPack? pack)
    {
        Requests.Add(assemblies);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<string> Pending => [];

    public event Action? Changed
    {
        add { }
        remove { }
    }
}

public class ComponentTests
{
    static BunitContext CreateContext(out StubToolLoader loader)
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddScoped(_ => new HttpClient { BaseAddress = new("http://localhost/") });
        context.Services.AddConvert(Convert.Web.Packs.All);
        var stub = new StubToolLoader();
        loader = stub;
        context.Services.AddScoped<IToolLoader>(_ => stub);
        return context;
    }

    [Test]
    public async Task WizardWalksFromEmptyToConverted()
    {
        await using var context = CreateContext(out var loader);
        var page = context.Render<Index>();
        await Assert.That(page.FindAll(".drop-zone").Count).IsEqualTo(1);

        var session = context.Services.GetRequiredService<ConvertSession>();
        await page.InvokeAsync(() => session.LoadTextAsync(FileTypes.Markdown, "# Hello\n\nSome *text*."));
        await page.WaitForAssertionAsync(() => page.Find(".target-picker"));
        await Assert.That(page.FindAll(".drop-zone").Count).IsEqualTo(0);
        // Markdig's preview asked for Markdig.
        await Assert.That(loader.Requests.Any(_ => _.Contains("Markdig.wasm"))).IsTrue();

        var html = page.FindAll(".chip").Single(_ => _.TextContent.Trim() == "HTML");
        await html.ClickAsync(new());
        // Two tools serve md→html: the picker shows both, Markdig first and selected.
        var tools = page.FindAll(".tool-choice .tool-name").Select(_ => _.TextContent).ToList();
        await Assert.That(tools).IsEquivalentTo(["Markdig", "OfficeIMO"]);
        await Assert.That(session.Conversion!.Id).IsEqualTo("md-html-markdig");

        await page.Find(".convert-button").ClickAsync(new());
        await page.WaitForAssertionAsync(() => page.Find(".download-button"));
        await Assert.That(session.Result!.Parts[0].Text).Contains("<h1");
        await Assert.That(page.Find(".download-button").TextContent).Contains("pasted.html");

        // An edit drops the cached result.
        await page.InvokeAsync(() => session.ReplaceSourceAsync(Document.FromText("pasted.md", "# Changed", FileTypes.Markdown)));
        await Assert.That(session.Result).IsNull();

        await page.Find(".start-over").ClickAsync(new());
        await Assert.That(page.FindAll(".drop-zone").Count).IsEqualTo(1);
    }

    [Test]
    public async Task UnknownFileShowsAnErrorWithoutIssueLink()
    {
        await using var context = CreateContext(out _);
        var page = context.Render<Index>();
        var session = context.Services.GetRequiredService<ConvertSession>();
        await page.InvokeAsync(() => session.LoadAsync("thing.xyz", [1, 2, 3]));
        await page.WaitForAssertionAsync(() => page.Find(".error"));
        await Assert.That(page.FindAll(".error-report").Count).IsEqualTo(0);
    }

    public static IEnumerable<string> OptionPanels() =>
        Fixtures.Registry.Conversions
            .Where(_ => _.OptionsComponent is not null)
            .GroupBy(_ => (_.OptionsComponent, Flags: System.Text.Json.JsonSerializer.Serialize(_.CreateOptions(), _.CreateOptions().GetType())))
            .Select(_ => _.First().Id)
            .Order(StringComparer.Ordinal);

    // Each options panel renders its defaults.
    [Test]
    [MethodDataSource(nameof(OptionPanels))]
    public async Task OptionsPanelDefaults(string id)
    {
        await using var context = CreateContext(out _);
        var conversion = Fixtures.Registry.Conversions.Single(_ => _.Id == id);
        var panel = context.Render<OptionsHost>(_ => _
            .Add(host => host.Conversion, conversion)
            .Add(host => host.Options, conversion.CreateOptions()));
        await Verify(panel.Markup, "html").UseParameters(id);
    }
}
