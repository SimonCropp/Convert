/// <summary>A file as a <c>data:</c> URL, for pasting into HTML or CSS.</summary>
sealed class DataUriConversion(FileType source) : Conversion<NoOptions>
{
    public override string Id => $"{source.Id}-datauri-dotnet";
    public override FileType Source => source;
    public override FileType Target => FileTypes.DataUri;
    public override Tool Tool => DataPack.DotNet;
    public override string Summary => "The whole file Base64-encoded in one data: URL, for src=\"…\" or url(…).";
    public override string Expect => "A third larger than the file itself: best for small icons and images.";
    public override IReadOnlyList<string> Assemblies => [];

    protected override Task<ConversionOutput> RunAsync(Document document, NoOptions options, IProgress<ConversionProgress> progress, Cancel cancel)
    {
        var uri = $"data:{source.ContentType};base64,{System.Convert.ToBase64String(document.Bytes)}";
        return Task.FromResult(
            new ConversionOutput(
                Document.FromText(
                    FileTypes.DataUri.FileNameFor(document.FileName),
                    uri,
                    FileTypes.DataUri)));
    }
}
