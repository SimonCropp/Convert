namespace Convert;

/// <summary>Every pack the app ships, in the order their samples and tools are listed.</summary>
public static class Packs
{
    public static IReadOnlyList<IPack> All =>
    [
        new MorphPack(),
        new BrowserPack(),
        new ImageSharpPack(),
        new SkiaPack(),
        new NaiadPack(),
        new GeoPack(),
        new DataPack(),
        new MarkdigPack(),
        new OpenXmlHtmlPack(),
        new OfficeImoPack(),
    ];
}
