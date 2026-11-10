static partial class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyPlaywright.Initialize(installPlaywright: true);
        VerifierSettings.UseSsimForPng(.7);
        VerifierSettings.InitializePlugins();

        // bUnit stamps a fresh element-reference GUID on InputFile each render.
        VerifierSettings.ScrubLinesWithReplace(_ =>
            ElementReference.Replace(_, "blazor:elementreference=\"scrubbed\""));

        VerifyTests.AngleSharp.HtmlPrettyPrint.All();

        // Blazor emits <!--!--> markers between components; they are noise in html snapshots.
        // Registered after HtmlPrettyPrint with ScrubberLocation.First so it runs before the pretty printer.
        VerifierSettings.AddScrubber("html", _ => _.Replace("<!--!-->", ""), ScrubberLocation.First);

        Fixtures.UseDesktopFonts();
    }

    [GeneratedRegex("blazor:elementreference=\"[^\"]*\"", RegexOptions.IgnoreCase)]
    private static partial Regex ElementReference { get; }
}
