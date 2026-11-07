namespace Convert;

public partial class MorphPngOptionsPanel
{
    // The lists Morph's own converter offers (ExportOptionChoices), restated with the pack's enum.
    static readonly IReadOnlyList<(int Value, string Label)> dpis =
    [
        (96, "96 DPI — screen"),
        (150, "150 DPI — default"),
        (300, "300 DPI — print")
    ];

    static readonly IReadOnlyList<(MorphCrop Value, string Label)> crops =
    [
        (MorphCrop.FullPage, "Whole page — default"),
        (MorphCrop.ContentBox, "Content only — margins cropped"),
        (MorphCrop.ContentBoxWithHeaderFooter, "Content with header and footer")
    ];
}
