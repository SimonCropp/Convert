namespace Convert.Skia;

/// <summary>
/// The faces SVG text is drawn with. Skia in the browser has no system fonts, so every family maps onto
/// Morph's bundled Aptos, fetched by URL (the files ship with the app anyway). Fetched once; a failure is
/// retried next time. Names no Skia type, so the pack can call it at startup.
/// </summary>
public static class SkiaFonts
{
    static readonly string[] files = ["Aptos_400.ttf", "Aptos_700.ttf", "Aptos_400_Italic.ttf", "Aptos_700_Italic.ttf"];

    static Task<Faces>? load;

    /// <summary>Reads a font by its path relative to the app base. Set by the pack, or by a desktop host.</summary>
    public static Func<string, Task<byte[]>>? Fetch { get; set; }

    internal static void Initialize(HttpClient http) =>
        Fetch ??= http.GetByteArrayAsync;

    internal static Task<Faces> EnsureAsync()
    {
        if (load is { IsCompleted: true, IsCompletedSuccessfully: false })
        {
            load = null;
        }

        return load ??= LoadAsync();
    }

    static async Task<Faces> LoadAsync()
    {
        var fetch = Fetch ?? throw new InvalidOperationException("The Skia pack has not been loaded.");
        var faces = await Task.WhenAll(files.Select(_ => fetch($"_content/Morph.Blazor/fonts/{_}")));
        return new(faces[0], faces[1], faces[2], faces[3]);
    }

    internal sealed record Faces(byte[] Regular, byte[] Bold, byte[] Italic, byte[] BoldItalic);
}
