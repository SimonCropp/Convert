namespace Convert;

/// <summary>PDF to docx/xlsx/pptx/html/png/txt/md, after OfficeIMO's own browser converter.</summary>
static class PdfImportRunner
{
    const int maxPages = 500;

    // One method per target, the dispatcher naming OfficeIMO.Pdf types only: see WordRunner for why.
    public static ConversionOutput Convert(Document document, FileType target, object options, OfficeImoFonts.FontSet fonts, Cancel cancel)
    {
        using var pdf = Open(document);
        cancel.ThrowIfCancellationRequested();
        var name = target.FileNameFor(document.FileName);
        return target.Id switch
        {
            "docx" => Word(pdf, name, cancel),
            "xlsx" => Excel(pdf, name, cancel),
            "pptx" => PowerPoint(pdf, name, (options as PdfSlidesOptions)?.Mode ?? SlideMode.EditableContent, fonts, cancel),
            "html" => Html(pdf, name, cancel),
            "png" => Png(pdf, document, (options as PdfPngOptions)?.Dpi ?? 144, fonts, cancel),
            "txt" => Text(pdf, name, cancel),
            "md" => Markdown(pdf, name, cancel),
            _ => throw new NotSupportedException($"OfficeIMO does not convert PDF to {target.DisplayName} here.")
        };
    }

    static ConversionOutput Word(PdfDocument pdf, string name, Cancel cancel)
    {
        using var word = pdf.ToWord(
            new()
            {
                FailOnLoss = false
            },
            out var report,
            cancel);
        return new([new(name, word.ToBytes(), FileTypes.Docx)], Reports.Notes(report));
    }

    static ConversionOutput Excel(PdfDocument pdf, string name, Cancel cancel)
    {
        using var excel = pdf.ToExcel(
            new()
            {
                FailOnLoss = false
            },
            out var report,
            cancel);
        List<string> notes = ["Only the tables detected in the PDF are imported.", .. Reports.Notes(report)];
        return new([new(name, excel.ToBytes(), FileTypes.Xlsx)], notes);
    }

    static ConversionOutput PowerPoint(PdfDocument pdf, string name, SlideMode mode, OfficeImoFonts.FontSet fonts, Cancel cancel)
    {
        using var presentation = pdf.ToPowerPoint(
            new()
            {
                FailOnLoss = false,
                Mode = Mode(mode),
                RenderFonts = fonts.Layout()
            },
            out var report,
            cancel);
        using var output = new MemoryStream();
        presentation.Save(output);
        return new([new(name, output.ToArray(), FileTypes.Pptx)], Reports.Notes(report));
    }

    static ConversionOutput Html(PdfDocument pdf, string name, Cancel cancel)
    {
        var html = pdf.ToHtml(
            new PdfHtmlExportOptions
            {
                FailOnLoss = false,
                Profile = PdfHtmlProfile.PositionedReview,
                IncludeLinkAnnotations = true,
                IncludeFormWidgets = true
            },
            out var report,
            cancel);
        return new([Document.FromText(name, html, FileTypes.Html)], Reports.Notes(report));
    }

    static ConversionOutput Text(PdfDocument pdf, string name, Cancel cancel)
    {
        var text = pdf.ExtractText(cancel);
        return new(Document.FromText(name, text, FileTypes.Text), Empty(text));
    }

    static ConversionOutput Markdown(PdfDocument pdf, string name, Cancel cancel)
    {
        var markdown = pdf.ExtractMarkdown(
            new()
            {
                IncludePageSeparators = true,
                IncludeLinkAnnotations = true
            },
            cancel);
        return new(Document.FromText(name, markdown, FileTypes.Markdown), Empty(markdown));
    }

    // A scanned PDF is page images: there is no text to extract without OCR.
    static List<string> Empty(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ["The PDF has no text layer (a scan, perhaps): nothing could be extracted."];
        }

        return [];
    }

    static ConversionOutput Png(PdfDocument pdf, Document document, int dpi, OfficeImoFonts.FontSet fonts, Cancel cancel)
    {
        var images = pdf.ExportImages(
            new PdfImageExportOptions
            {
                FailOnLoss = false,
                Format = OfficeImageExportFormat.Png,
                TargetDpi = dpi,
                Fonts = fonts.Layout(),
                MaximumOutputCount = maxPages,
                MaximumTotalRasterPixels = 250_000_000L,
                RenderTimeout = TimeSpan.FromSeconds(30),
                MaximumDegreeOfParallelism = 1
            },
            out var report,
            cancel);
        if (images.Count == 0)
        {
            throw new UserInputException("The PDF has no pages to render.");
        }

        var notes = Reports.Notes(report);
        notes.AddRange(images.SelectMany((image, index) => image.Diagnostics.Select(_ => $"Page {index + 1}: {_.Message}")));
        if (images.Count == 1)
        {
            return new([new(FileTypes.Png.FileNameFor(document.FileName), images[0].Bytes, FileTypes.Png)], notes);
        }

        return new([.. images.Select((_, index) => new Document(ZipParts.PageName(index, FileTypes.Png), _.Bytes, FileTypes.Png))], notes);
    }

    static PdfDocument Open(Document document)
    {
        using var input = new MemoryStream(document.Bytes, writable: false);
        try
        {
            return PdfDocument.Load(
                input,
                new()
                {
                    AesCryptographyProvider = OfficeManagedAesCryptographyProvider.Default,
                    Limits = new()
                    {
                        MaxInputBytes = 25L * 1024 * 1024,
                        MaxPages = maxPages
                    }
                });
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException)
        {
            throw new UserInputException($"The PDF could not be read: {exception.Message}", exception);
        }
    }

    static PowerPointPdfImportMode Mode(SlideMode mode) =>
        mode switch
        {
            SlideMode.VisualPages => PowerPointPdfImportMode.VisualPages,
            SlideMode.HybridVisualAndEditableTables => PowerPointPdfImportMode.HybridVisualAndEditableTables,
            SlideMode.EditableTables => PowerPointPdfImportMode.EditableTables,
            _ => PowerPointPdfImportMode.EditableContent
        };
}