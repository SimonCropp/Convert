namespace Convert.Markdig.Previews;

/// <summary>Markdown rendered by Markdig into the core sandboxed frame.</summary>
public partial class MarkdownPreview
{
    string html = "";

    protected override void OnParametersSet() =>
        html = MarkdigRunner.Preview(Document.Text);
}
