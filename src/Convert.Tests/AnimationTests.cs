using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>ImageSharp keeps every frame between the animated formats (GIF, WebP, APNG).</summary>
public class AnimationTests
{
    [Test]
    [Arguments("gif-webp-imagesharp")]
    [Arguments("gif-png-imagesharp")]
    [Arguments("gif-tiff-imagesharp")]
    public async Task FramesCarryOver(string id)
    {
        var conversion = Fixtures.Registry.Conversions.Single(_ => _.Id == id);
        var source = new Document("animated.gif", AnimatedGif(), FileTypes.Gif);
        var output = await conversion.RunAsync(source, conversion.CreateOptions(), new Progress<ConversionProgress>(), Cancel.None);
        using var result = Image.Load(output.Parts[0].Bytes);
        await Assert.That(result.Frames.Count).IsEqualTo(3);
    }

    // Three 8×8 frames, red, green and blue.
    static byte[] AnimatedGif()
    {
        using var image = new Image<Rgba32>(8, 8, new(255, 0, 0));
        foreach (var color in new[] { new Rgba32(0, 255, 0), new Rgba32(0, 0, 255) })
        {
            using var frame = new Image<Rgba32>(8, 8, color);
            image.Frames.AddFrame(frame.Frames.RootFrame);
        }

        using var stream = new MemoryStream();
        image.Save(stream, new GifEncoder());
        return stream.ToArray();
    }
}
