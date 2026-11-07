namespace Convert.Web;

/// <summary>Every pack the app ships, in the order their samples and tools are listed.</summary>
public static class Packs
{
    public static IReadOnlyList<IPack> All =>
    [
        new Morph.MorphPack(),
        new Browser.BrowserPack(),
        new ImageSharp.ImageSharpPack(),
        new Skia.SkiaPack(),
        new Naiad.NaiadPack(),
        new Geo.GeoPack(),
        new Markdig.MarkdigPack(),
        new OpenXmlHtml.OpenXmlHtmlPack(),
        new OfficeIMO.OfficeImoPack(),
    ];
}
