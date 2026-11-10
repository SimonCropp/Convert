public enum PdfProfile
{
    /// <summary>PDF 1.7 with embedded fonts.</summary>
    Standard,

    /// <summary>PDF/A-2b, for long-term archiving.</summary>
    Archive,

    /// <summary>PDF/UA-1, tagged for assistive technology.</summary>
    Accessible
}

/// <summary>Mirrors OfficeIMO's <c>PowerPointPdfImportMode</c>.</summary>
public enum SlideMode
{
    EditableContent,
    VisualPages,
    HybridVisualAndEditableTables,
    EditableTables
}

public sealed class OfficeImoPdfOptions
{
    public PdfProfile Profile { get; set; } = PdfProfile.Standard;

    /// <summary>Excel only: up to 250 rows per sheet, for a quick look at a large workbook.</summary>
    public bool QuickPreview { get; set; }

    /// <summary>Set by the conversion: show <see cref="QuickPreview"/>.</summary>
    public bool Excel { get; set; }
}

public sealed class PdfSlidesOptions
{
    public SlideMode Mode { get; set; } = SlideMode.EditableContent;
}

public sealed class PdfPngOptions
{
    public int Dpi { get; set; } = 144;
}

static class OfficeImoChoices
{
    public static readonly IReadOnlyList<(PdfProfile Value, string Label)> Profiles =
    [
        (PdfProfile.Standard, "Standard PDF 1.7"),
        (PdfProfile.Archive, "Archive (PDF/A-2b)"),
        (PdfProfile.Accessible, "Accessible (PDF/UA-1)")
    ];

    public static readonly IReadOnlyList<(SlideMode Value, string Label)> SlideModes =
    [
        (SlideMode.EditableContent, "Editable text, shapes and tables"),
        (SlideMode.VisualPages, "Page images — looks exact, not editable"),
        (SlideMode.HybridVisualAndEditableTables, "Page images with editable tables"),
        (SlideMode.EditableTables, "Detected tables only")
    ];

    public static readonly IReadOnlyList<(int Value, string Label)> Dpis =
    [
        (72, "72 DPI — thumbnail"),
        (144, "144 DPI — default"),
        (300, "300 DPI — print")
    ];
}
