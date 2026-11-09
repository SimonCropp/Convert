namespace Convert;

/// <summary>
/// The document's text in a <c>&lt;pre&gt;</c>. JSON and XML are shown laid out by
/// <see cref="PrettyPrint"/>; the document itself is unchanged.
/// </summary>
public partial class TextPreview
{
    const int limit = 200_000;

    string text = "";

    bool Truncated => text.Length > limit;

    string Shown
    {
        get
        {
            if (Truncated)
            {
                return text[..limit];
            }

            return text;
        }
    }

    protected override void OnParametersSet()
    {
        if (PrettyPrint.Supports(Document.Type))
        {
            text = PrettyPrint.Apply(Document, true).Text;
            return;
        }

        text = Document.Text;
    }
}
