using OfficeIMO;
using OfficeIMO.Excel;
using OfficeIMO.Excel.Pdf;
using OfficeIMO.Html;
using OfficeIMO.Html.Pdf;
using OfficeIMO.Pdf;
using OfficeIMO.PowerPoint;
using OfficeIMO.PowerPoint.Pdf;
using OfficeIMO.Publisher;
using OfficeIMO.Publisher.Pdf;
using OfficeIMO.Word;
using OfficeIMO.Word.Pdf;

namespace Convert.OfficeIMO;

/// <summary>
/// docx/xlsx/pptx/pub/html to PDF, after OfficeIMO's own browser converter.
///
/// One method per format, and the dispatcher names only OfficeIMO.Core and OfficeIMO.Pdf types (which
/// every route loads): Mono resolves a method's locals when it first interprets it, so a single switch
/// holding an ExcelDocument local would make Word to PDF load OfficeIMO.Excel.
/// </summary>
static class WordRunner
{
    // Browser budgets OfficeIMO's converter uses.
    const long maxPackageBytes = 25L * 1024 * 1024;
    const int maxPreviewRowsPerSheet = 250;

    public static (byte[] Bytes, IReadOnlyList<string> Notes) ToPdf(Document document, OfficeImoPdfOptions options, OfficeImoFonts.FontSet fonts, Cancel cancel)
    {
        var pdfOptions = PdfProfiles.Create(options.Profile, fonts);
        OfficeConversionReport report;
        using var pdf = document.Type.Id switch
        {
            "docx" => Word(document, pdfOptions, out report, cancel),
            "xlsx" => Excel(document, pdfOptions, options.QuickPreview, out report, cancel),
            "pptx" => PowerPoint(document, pdfOptions, out report, cancel),
            "pub" => Publisher(document, pdfOptions, fonts, out report, cancel),
            "html" => Html(document, pdfOptions, fonts, out report, cancel),
            _ => throw new NotSupportedException($"OfficeIMO does not convert {document.Type.DisplayName} to PDF here.")
        };
        cancel.ThrowIfCancellationRequested();
        var bytes = pdf.ToBytes(new PdfSaveOptions { AllowLoss = true }, cancel);
        return (bytes, Reports.Notes(report));
    }

    static PdfDocument Word(Document document, PdfCreateOptions pdfOptions, out OfficeConversionReport report, Cancel cancel)
    {
        using var stream = new MemoryStream(document.Bytes, writable: false);
        using var word = WordDocument.Load(stream, new WordLoadOptions { MaxInputBytes = maxPackageBytes });
        return word.ToPdf(
            new WordPdfExportOptions
            {
                FailOnLoss = false,
                PdfOptions = pdfOptions,
                FontFamily = OfficeImoFonts.Family,
                ResourcePolicy = PdfResourcePolicy.CreatePortableDeterministic()
            },
            out report,
            cancel);
    }

    static PdfDocument Excel(Document document, PdfCreateOptions pdfOptions, bool quickPreview, out OfficeConversionReport report, Cancel cancel)
    {
        using var stream = new MemoryStream(document.Bytes, writable: false);
        using var excel = ExcelDocument.Load(stream, new ExcelLoadOptions { AccessMode = DocumentAccessMode.ReadOnly, PackageSecurity = Security() });
        return excel.ToPdf(
            new ExcelPdfExportOptions
            {
                FailOnLoss = false,
                PdfOptions = pdfOptions,
                FontFamily = OfficeImoFonts.Family,
                ResourcePolicy = PdfResourcePolicy.CreatePortableDeterministic(),
                MaxRowsPerSheet = quickPreview ? maxPreviewRowsPerSheet : null,
                UseBoundedWorksheetRead = quickPreview
            },
            out report,
            cancel);
    }

    static PdfDocument PowerPoint(Document document, PdfCreateOptions pdfOptions, out OfficeConversionReport report, Cancel cancel)
    {
        using var stream = new MemoryStream(document.Bytes, writable: false);
        using var presentation = PowerPointDocument.Load(stream, new PowerPointLoadOptions { AccessMode = DocumentAccessMode.ReadOnly, PackageSecurity = Security() });
        return presentation.ToPdf(
            new PowerPointPdfExportOptions
            {
                FailOnLoss = false,
                PdfOptions = pdfOptions,
                FontFamily = OfficeImoFonts.Family,
                ResourcePolicy = PdfResourcePolicy.CreatePortableDeterministic(),
                WarnOnPictureAspectRatioDistortion = true
            },
            out report,
            cancel);
    }

    static PdfDocument Publisher(Document document, PdfCreateOptions pdfOptions, OfficeImoFonts.FontSet fonts, out OfficeConversionReport report, Cancel cancel)
    {
        var load = new PublisherLoadOptions
        {
            Limits = { MaxInputBytes = (int) maxPackageBytes },
            MaxPages = 128,
            MaxTotalImageBytes = 32 * 1024 * 1024,
            Fonts = fonts.Layout()
        };
        var publication = PublisherDocument.Load(document.Bytes, load, cancel);
        return publication.ToPdf(new PublisherPdfExportOptions { PdfOptions = pdfOptions, FailOnLoss = false }, out report, cancel);
    }

    static PdfDocument Html(Document document, PdfCreateOptions pdfOptions, OfficeImoFonts.FontSet fonts, out OfficeConversionReport report, Cancel cancel) =>
        HtmlConversionDocument.Parse(document.Text).ToPdf(
            new HtmlPdfExportOptions
            {
                FailOnLoss = false,
                PdfOptions = pdfOptions,
                FontFamily = fonts.PdfFamily(),
                TextShapingMode = PdfTextShapingMode.LatinLigatures,
                ResourcePolicy = PdfResourcePolicy.CreatePortableDeterministic()
            },
            out report,
            cancel);

    static OfficePackageSecurityOptions Security()
    {
        var options = OfficePackageSecurityOptions.SecureDefaults;
        options.MaxPackageBytes = maxPackageBytes;
        options.MaxPartCount = 5_000;
        options.MaxPartUncompressedBytes = 32L * 1024 * 1024;
        options.MaxTotalUncompressedBytes = 128L * 1024 * 1024;
        options.MaxCompressionRatio = 200;
        return options;
    }
}

/// <summary>OfficeIMO diagnostics as result notes. Names only OfficeIMO.Core, which every route loads.</summary>
static class Reports
{
    public static List<string> Notes(OfficeConversionReport? report) =>
        report is null ? [] : [.. report.Diagnostics.Select(_ => _.ToString() ?? _.Message).Distinct()];
}

/// <summary>The PDF writer settings, per profile, with the Aptos faces embedded for every family.</summary>
static class PdfProfiles
{
    public static PdfCreateOptions Create(PdfProfile profile, OfficeImoFonts.FontSet fonts)
    {
        var options = new PdfCreateOptions
        {
            DefaultFont = PdfStandardFont.Helvetica,
            FileVersion = PdfFileVersion.Pdf17,
            ObjectSerializationMode = PdfObjectSerializationMode.ForwardOnly,
            TaggedStructureMode = PdfTaggedStructureMode.CatalogMarkers,
            TextShapingMode = PdfTextShapingMode.LatinLigatures
        };
        switch (profile)
        {
            case PdfProfile.Archive:
                options.UsePdfA(PdfComplianceProfile.PdfA2B, "und").RequireCompliance(PdfComplianceProfile.PdfA2B);
                break;
            case PdfProfile.Accessible:
                options.UsePdfUa(PdfComplianceProfile.PdfUa1, "und");
                break;
        }

        var family = fonts.PdfFamily();
        options.RegisterFontFamily(PdfStandardFont.Helvetica, family);
        options.RegisterNamedFontFamily(family);
        foreach (var alias in OfficeImoFonts.FontSet.Aliases)
        {
            options.RegisterFontFamilySubstitution(alias, OfficeImoFonts.Family);
        }

        return options;
    }
}
