namespace Convert.Geo.Previews;

/// <summary>The features drawn by GeoConvert, shown through the core SVG or image preview.</summary>
public partial class MapPreview
{
    Document? map;
    Exception? error;
    Document? shown;

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
            map = GeoRunner.Preview(Document);
        }
        catch (Exception exception)
        {
            map = null;
            error = exception;
        }
    }
}
