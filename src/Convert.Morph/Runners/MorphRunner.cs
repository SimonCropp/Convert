using Morph;

/// <summary>The conversions' only route into Morph.</summary>
static class MorphRunner
{
    public static async Task<ConversionOutput> RunAsync(Document document, FileType target, MorphPngOptions? png, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        progress.Report(new("Loading fonts"));
        var fonts = await MorphFonts.DirectoryAsync();
        progress.Report(new("Converting", $"{document.Type.ShortName} to {target.ShortName}"));
        var format = Format(document.Type);
        var bytes = document.Bytes;
        var name = target.FileNameFor(document.FileName);

        switch (target.Id)
        {
            case "png":
                var settings = new ImageSettings
                {
                    Dpi = png?.Dpi ?? 150,
                    Crop = (png?.Crop ?? MorphCrop.FullPage) switch
                    {
                        MorphCrop.ContentBox => PageCrop.ContentBox,
                        MorphCrop.ContentBoxWithHeaderFooter => PageCrop.ContentBoxWithHeaderFooter,
                        _ => PageCrop.FullPage
                    }
                };
                var pages = ConversionService.RenderPngPages(bytes, format, settings, fonts, cancel);
                if (pages.Count == 1)
                {
                    return new(new(name, pages[0], FileTypes.Png));
                }

                return new([.. pages.Select((_, index) => new Document(ZipParts.PageName(index, FileTypes.Png), _, FileTypes.Png))], []);
            case "pdf":
                return new(new(name, ConversionService.ToPdf(bytes, format, fonts, cancel), FileTypes.Pdf));
            case "html":
                return new(Document.FromText(name, ConversionService.ToHtml(bytes, format, fonts), FileTypes.Html));
            case "md":
                return new(Document.FromText(name, ConversionService.ToMarkdown(bytes, format, fonts), FileTypes.Markdown));
            case "txt":
                return new(Document.FromText(name, ConversionService.ToText(bytes, format, fonts), FileTypes.Text));
            default:
                throw new NotSupportedException($"Morph does not write {target.DisplayName}.");
        }
    }

    static InputFormat Format(FileType type) =>
        type.Id switch
        {
            "docx" => InputFormat.Docx,
            "xlsx" => InputFormat.Xlsx,
            "pptx" => InputFormat.Pptx,
            _ => throw new NotSupportedException($"Morph does not read {type.DisplayName}.")
        };
}
