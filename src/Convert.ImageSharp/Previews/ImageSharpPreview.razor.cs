namespace Convert.ImageSharp.Previews;

/// <summary>Decodes the image with ImageSharp and shows it through the core image preview.</summary>
public partial class ImageSharpPreview
{
    Document? png;
    Document? shown;
    Exception? error;

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(shown, Document))
        {
            return;
        }

        shown = Document;
        error = null;
        try
        {
            png = new(FileTypes.Png.FileNameFor(Document.FileName), ImageSharpRunner.Preview(Document.Bytes), FileTypes.Png);
        }
        catch (UserInputException exception)
        {
            png = null;
            error = exception;
        }
    }
}
