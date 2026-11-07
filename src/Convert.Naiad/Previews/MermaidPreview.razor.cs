namespace Convert.Naiad.Previews;

/// <summary>The diagram rendered by Naiad, shown through the core SVG preview.</summary>
public partial class MermaidPreview
{
    Document? svg;
    Exception? error;

    protected override void OnParametersSet()
    {
        error = null;
        try
        {
            var markup = MermaidRunner.Preview(Document.Text);
            svg = Document.FromText(FileTypes.Svg.FileNameFor(Document.FileName), markup, FileTypes.Svg);
        }
        catch (UserInputException exception)
        {
            // The last good diagram (if any) stays, marked stale: the error says why this text does not render.
            error = exception;
        }
    }
}
