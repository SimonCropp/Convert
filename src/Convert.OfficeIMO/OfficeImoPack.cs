namespace Convert;

/// <summary>
/// The 13 routes OfficeIMO proves in the browser: Office and Publisher to PDF, HTML to PDF, PDF to Office,
/// HTML and PNG, and Markdown to and from HTML and Word.
/// </summary>
public sealed class OfficeImoPack : IPack
{
    public static Tool Tool { get; } = new(
        "officeimo",
        "OfficeIMO",
        "Reads and writes Office, PDF, HTML and Markdown in pure .NET, with fidelity reports.",
        "https://github.com/EvotecIT/OfficeIMO");

    const string core = "OfficeIMO.Core.wasm";
    const string pdf = "OfficeIMO.Pdf.wasm";
    const string word = "OfficeIMO.Word.wasm";
    const string excel = "OfficeIMO.Excel.wasm";
    const string powerPoint = "OfficeIMO.PowerPoint.wasm";
    const string markdown = "OfficeIMO.Markdown.wasm";
    const string htmlCore = "OfficeIMO.Html.Core.wasm";

    static readonly string[] openXml = ["DocumentFormat.OpenXml.wasm", "DocumentFormat.OpenXml.Framework.wasm", "System.IO.Packaging.wasm"];
    static readonly string[] html = ["OfficeIMO.Html.wasm", htmlCore, "OfficeIMO.Html.AngleSharp.wasm", "AngleSharp.wasm", "AngleSharp.Css.wasm"];

    internal static IReadOnlyList<string> WordPdf { get; } = [core, word, .. openXml, pdf, "OfficeIMO.Word.Pdf.wasm"];
    internal static IReadOnlyList<string> ExcelPdf { get; } = [core, excel, .. openXml, pdf, "OfficeIMO.Excel.Pdf.wasm"];
    internal static IReadOnlyList<string> PowerPointPdf { get; } = [core, powerPoint, .. openXml, pdf, "OfficeIMO.PowerPoint.Pdf.wasm"];
    internal static IReadOnlyList<string> PublisherPdf { get; } = [core, "OfficeIMO.Publisher.wasm", pdf, "OfficeIMO.Publisher.Pdf.wasm"];
    internal static IReadOnlyList<string> HtmlPdf { get; } = [core, .. html, pdf, "OfficeIMO.Html.Pdf.wasm"];
    internal static IReadOnlyList<string> MarkdownHtml { get; } = [core, markdown, "OfficeIMO.MarkdownRenderer.wasm", "OfficeIMO.Markdown.Html.wasm", .. html];
    internal static IReadOnlyList<string> HtmlMarkdown { get; } = [core, markdown, "OfficeIMO.Markdown.Html.wasm", .. html];
    internal static IReadOnlyList<string> MarkdownWord { get; } = [core, word, .. openXml, markdown, "OfficeIMO.Word.Markdown.wasm"];
    internal static IReadOnlyList<string> PdfWord { get; } = [core, word, .. openXml, pdf, "OfficeIMO.Pdf.Word.wasm"];
    internal static IReadOnlyList<string> PdfExcel { get; } = [core, excel, .. openXml, pdf, "OfficeIMO.Pdf.Excel.wasm"];
    internal static IReadOnlyList<string> PdfPowerPoint { get; } = [core, powerPoint, .. openXml, pdf, "OfficeIMO.Pdf.PowerPoint.wasm"];
    internal static IReadOnlyList<string> PdfHtml { get; } = [core, pdf, .. html, "OfficeIMO.Html.Pdf.wasm"];
    internal static IReadOnlyList<string> PdfPng { get; } = [core, pdf];

    public void Register(ConversionRegistry registry)
    {
        registry.Add(FileTypes.Pub);

        registry.Add(new ToPdf(FileTypes.Docx, WordPdf, "Word's own layout model rebuilt as PDF, with a fidelity report."));
        registry.Add(new ToPdf(FileTypes.Xlsx, ExcelPdf, "Each worksheet as printed pages, with a fidelity report."));
        registry.Add(new ToPdf(FileTypes.Pptx, PowerPointPdf, "One page per slide, with a fidelity report."));
        registry.Add(new ToPdf(FileTypes.Pub, PublisherPdf, "Publisher pages at their own size, with a fidelity report."));
        registry.Add(new ToPdf(FileTypes.Html, HtmlPdf, "HTML and CSS laid out as paged PDF."));

        registry.Add(new MarkdownToHtml());
        registry.Add(new HtmlToMarkdown());
        registry.Add(new MarkdownToDocx());

        registry.Add(new FromPdf(FileTypes.Docx, PdfWord, "Rebuilds paragraphs, headings and tables as an editable document."));
        registry.Add(new FromPdf(FileTypes.Xlsx, PdfExcel, "The tables it detects, one per worksheet."));
        registry.Add(new FromPdf(FileTypes.Pptx, PdfPowerPoint, "One slide per page: editable, as images, or both."));
        registry.Add(new FromPdf(FileTypes.Html, PdfHtml, "Positioned HTML for review, with links and form fields."));
        registry.Add(new FromPdf(FileTypes.Png, PdfPng, "Each page drawn in .NET at the chosen resolution."));
    }

    public void Loaded(IServiceProvider services) =>
        OfficeImoFonts.Initialize(services.GetRequiredService<HttpClient>());
}
