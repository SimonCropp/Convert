namespace Convert.Previews;

/// <summary>The document's text in a <c>&lt;pre&gt;</c>.</summary>
public partial class TextPreview
{
    const int limit = 200_000;

    string text = "";

    bool Truncated => text.Length > limit;

    string Shown => Truncated ? text[..limit] : text;

    protected override void OnParametersSet() =>
        text = Document.Text;
}
