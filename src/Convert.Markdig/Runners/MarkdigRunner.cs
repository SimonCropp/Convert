/// <summary>The only place Markdig's namespace is used.</summary>
static class MarkdigRunner
{
    public static string ToHtml(string markdown, MarkdownHtmlOptions options)
    {
        var builder = new MarkdownPipelineBuilder();
        if (options.AdvancedExtensions)
        {
            builder.UseAdvancedExtensions();
        }
        else if (options.PipeTables)
        {
            builder.UsePipeTables();
        }

        if (options.SoftLineBreakAsHard)
        {
            builder.UseSoftlineBreakAsHardlineBreak();
        }

        if (options.Emoji)
        {
            builder.UseEmojiAndSmiley();
        }

        var body = Markdown.ToHtml(markdown, builder.Build());
        var html = options.FullDocument ? Wrap(body) : body;
        return options.PrettyPrint ? PrettyPrint.Html(html) : html;
    }

    /// <summary>The preview: the default pipeline, as a full document.</summary>
    public static string Preview(string markdown) =>
        ToHtml(markdown, new());

    static string Wrap(string body) =>
        $$"""
          <!DOCTYPE html>
          <html lang="en">
          <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <style>
          body { font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif; line-height: 1.55; max-width: 46rem; margin: 1.5rem auto; padding: 0 1rem; color: #222; }
          pre, code { font-family: ui-monospace, SFMono-Regular, Consolas, monospace; font-size: 0.9em; }
          pre { background: #f5f5f5; padding: 0.75rem; overflow: auto; border-radius: 4px; }
          table { border-collapse: collapse; } th, td { border: 1px solid #ccc; padding: 0.3rem 0.6rem; }
          blockquote { margin-left: 0; padding-left: 1rem; border-left: 3px solid #ccc; color: #555; }
          img { max-width: 100%; }
          </style>
          </head>
          <body>
          {{body}}
          </body>
          </html>
          """;
}
