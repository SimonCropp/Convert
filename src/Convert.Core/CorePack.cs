namespace Convert;

/// <summary>The formats and previews every browser handles with the DOM alone.</summary>
public sealed class CorePack : IPack
{
    public void Register(ConversionRegistry registry)
    {
        registry.Add(FileTypes.Text);
        registry.Add(FileTypes.Html);
        registry.Add(FileTypes.Markdown);
        registry.Add(FileTypes.Svg);
        registry.Add(FileTypes.Png);
        registry.Add(FileTypes.Jpeg);
        registry.Add(FileTypes.WebP);
        registry.Add(FileTypes.Gif);
        registry.Add(FileTypes.Bmp);
        registry.Add(FileTypes.Ico);

        registry.Add(new Preview(FileTypes.Text, [], typeof(Previews.TextPreview)));
        // Markdig's rendered preview wins; this is the fallback when that pack is absent.
        registry.Add(new Preview(FileTypes.Markdown, [], typeof(Previews.TextPreview), Priority: 100));
        registry.Add(new Preview(FileTypes.Html, [], typeof(Previews.FramePreview)));
        registry.Add(new Preview(FileTypes.Svg, [], typeof(Previews.SvgPreview)));
        registry.Add(new Preview(FileTypes.Png, [], typeof(Previews.ImagePreview)));
        registry.Add(new Preview(FileTypes.Jpeg, [], typeof(Previews.ImagePreview)));
        registry.Add(new Preview(FileTypes.WebP, [], typeof(Previews.ImagePreview)));
        registry.Add(new Preview(FileTypes.Gif, [], typeof(Previews.ImagePreview)));
        registry.Add(new Preview(FileTypes.Bmp, [], typeof(Previews.ImagePreview)));
        registry.Add(new Preview(FileTypes.Ico, [], typeof(Previews.ImagePreview)));
        registry.Add(new Preview(FileTypes.Zip, [], typeof(Previews.ZipPreview)));
    }
}
