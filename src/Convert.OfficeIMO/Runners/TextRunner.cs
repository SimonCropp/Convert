using OfficeIMO.Word;

namespace Convert;

/// <summary>Plain text to Word: one paragraph per line.</summary>
static class TextRunner
{
    const int maxTextChars = 500_000;

    public static byte[] ToDocx(string text, Cancel cancel)
    {
        if (text.Length > maxTextChars)
        {
            throw new UserInputException($"Text input is limited to {maxTextChars:N0} characters in the browser.");
        }

        using var word = WordDocument.Create();
        foreach (var line in text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n'))
        {
            cancel.ThrowIfCancellationRequested();
            word.AddParagraph(line);
        }

        return word.ToBytes();
    }
}
