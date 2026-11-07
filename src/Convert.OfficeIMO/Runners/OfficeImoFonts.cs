using OfficeIMO.Drawing;
using OfficeIMO.Pdf;

namespace Convert.OfficeIMO;

/// <summary>
/// The faces every OfficeIMO render embeds: Morph's bundled Aptos, fetched by URL (the files ship with
/// the app anyway, and a URL needs no Morph assembly). Fetched once; a failure is retried next time.
/// </summary>
public static class OfficeImoFonts
{
    internal const string Family = "Aptos";

    static readonly string[] files = ["Aptos_400.ttf", "Aptos_700.ttf", "Aptos_400_Italic.ttf", "Aptos_700_Italic.ttf"];

    static readonly string[] aliases =
    [
        "Arial", "Helvetica", "Calibri", "Carlito", "Segoe UI", "Tahoma", "Verdana", "sans", "sans-serif",
        "ui-sans-serif", "system-ui", "-apple-system", "BlinkMacSystemFont",
        "Times", "Times Roman", "Times-Roman", "Times New Roman", "serif", "Cambria", "Georgia",
        "Courier", "Courier New", "Consolas", "monospace", "ui-monospace"
    ];

    static Task<FontSet>? load;

    /// <summary>Reads a font by its path relative to the app base. Set by the pack, or by a desktop host.</summary>
    public static Func<string, Task<byte[]>>? Fetch { get; set; }

    internal static void Initialize(HttpClient http) =>
        Fetch ??= http.GetByteArrayAsync;

    internal static Task<FontSet> EnsureAsync()
    {
        if (load is { IsCompleted: true, IsCompletedSuccessfully: false })
        {
            load = null;
        }

        return load ??= LoadAsync();
    }

    static async Task<FontSet> LoadAsync()
    {
        var fetch = Fetch ?? throw new InvalidOperationException("The OfficeIMO pack has not been loaded.");
        var faces = await Task.WhenAll(files.Select(_ => fetch($"_content/Morph.Blazor/fonts/{_}")));
        return new(faces[0], faces[1], faces[2], faces[3]);
    }

    internal sealed record FontSet(byte[] Regular, byte[] Bold, byte[] Italic, byte[] BoldItalic)
    {
        public PdfEmbeddedFontFamily PdfFamily() =>
            new(Family, Regular, Bold, Italic, BoldItalic);

        /// <summary>A fresh collection each call: OfficeIMO may add to the one it is handed.</summary>
        public OfficeFontFaceCollection Layout()
        {
            var fonts = new OfficeFontFaceCollection()
                .Add(Family, Regular)
                .Add(Family, Bold, OfficeFontStyle.Bold)
                .Add(Family, Italic, OfficeFontStyle.Italic)
                .Add(Family, BoldItalic, OfficeFontStyle.Bold | OfficeFontStyle.Italic);
            foreach (var alias in aliases)
            {
                fonts.AddAlias(alias, Family);
            }

            return fonts;
        }

        public static IEnumerable<string> Aliases => aliases;
    }
}
