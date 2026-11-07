namespace Convert;

/// <summary>Mirrors Morph's <c>PageCrop</c>, so the options type needs no Morph assembly to lay out.</summary>
public enum MorphCrop
{
    FullPage,
    ContentBox,
    ContentBoxWithHeaderFooter
}

public sealed class MorphPngOptions
{
    public int Dpi { get; set; } = 150;

    public MorphCrop Crop { get; set; } = MorphCrop.FullPage;
}
