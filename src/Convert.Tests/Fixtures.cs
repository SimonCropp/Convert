/// <summary>
/// Paths and files the tests share. Samples and fonts are read from the app as published
/// (bin/{Configuration}/blazor-publish/wwwroot), so they are exactly what the browser serves.
/// </summary>
static class Fixtures
{
    public static string Wwwroot { get; } =
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(Fixtures).Assembly.Location)!, "..", "blazor-publish", "wwwroot"));

    public static string RepoRoot { get; } = FindRepoRoot();

    public static string SrcDirectory => Path.Combine(RepoRoot, "src");

    static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("No global.json above the test output.");
    }

    public static byte[] Asset(string relativeUrl) =>
        File.ReadAllBytes(Path.Combine(Wwwroot, relativeUrl.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>
    /// Points Morph and OfficeIMO at the published Aptos faces instead of the browser's in-memory
    /// filesystem and HttpClient.
    /// </summary>
    public static void UseDesktopFonts()
    {
        var fonts = Path.Combine(Wwwroot, "_content", "Morph.Blazor", "fonts");
        Convert.Morph.MorphFonts.Provider = () => Task.FromResult(fonts);
        Convert.OfficeIMO.OfficeImoFonts.Fetch = _ => Task.FromResult(Asset(_));
        Convert.Skia.SkiaFonts.Fetch = _ => Task.FromResult(Asset(_));
    }

    public static ConversionRegistry Registry { get; } = ConvertServiceCollectionExtensions.BuildRegistry(Convert.Web.Packs.All);
}
