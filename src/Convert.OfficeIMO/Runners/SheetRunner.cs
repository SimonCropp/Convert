using System.Globalization;
using System.Data;
using OfficeIMO.Excel;

namespace Convert;

/// <summary>Worksheets to and from CSV and TSV, through OfficeIMO.Excel. Loads OfficeIMO.Excel and OpenXml only.</summary>
static class SheetRunner
{
    // Excel's limit, and the characters it refuses in a sheet name.
    const int maxSheetName = 31;

    // The digits a double, and so an Excel number, holds exactly.
    const int maxNumberDigits = 15;
    static readonly char[] badSheetCharacters = ['[', ']', ':', '*', '?', '/', '\\'];

    /// <summary>One worksheet holding the records, the first row bold; numbers and true/false as typed cells.</summary>
    public static byte[] FromDelimited(string text, char delimiter, string fileName)
    {
        var records = Delimited.Read(text, delimiter);
        using var output = new MemoryStream();
        using (var document = ExcelDocument.Create())
        {
            var sheet = document.AddSheet(SheetName(fileName));
            var cells = new List<(int, int, object)>();
            for (var row = 0; row < records.Count; row++)
            {
                for (var column = 0; column < records[row].Count; column++)
                {
                    if (Cell(records[row][column], row == 0) is { } value)
                    {
                        cells.Add((row + 1, column + 1, value));
                    }
                }
            }

            sheet.SetValues(cells);
            if (records.Count > 0)
            {
                for (var column = 0; column < records[0].Count; column++)
                {
                    sheet.Cell(1, column + 1).SetBold();
                }
            }

            document.Save(output);
        }

        return output.ToArray();
    }

    // Header cells stay text; elsewhere an integer, a decimal or true/false is typed, but "007" (a code,
    // not a number) stays text, as does a number longer than Excel keeps (an id, a card number) and
    // anything empty.
    static object? Cell(string text, bool header)
    {
        if (text.Length == 0)
        {
            return null;
        }

        if (header)
        {
            return text;
        }

        if (bool.TryParse(text, out var flag))
        {
            return flag;
        }

        var leadingZero = text.Length > 1 && text[0] == '0' && char.IsAsciiDigit(text[1]);
        var digits = text.Count(char.IsAsciiDigit);
        if (!leadingZero && digits <= maxNumberDigits && char.IsAsciiDigit(text[^1]))
        {
            if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
            {
                return integer;
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
            {
                return number;
            }
        }

        return text;
    }

    static string SheetName(string fileName)
    {
        var name = new string([.. Path.GetFileNameWithoutExtension(fileName).Where(_ => !badSheetCharacters.Contains(_))]).Trim('\'', ' ');
        if (name.Length == 0)
        {
            return "Sheet1";
        }

        return name.Length > maxSheetName ? name[..maxSheetName] : name;
    }

    /// <summary>Every worksheet as delimited text, in workbook order, with its name.</summary>
    public static List<(string Sheet, string Text)> ToDelimited(byte[] bytes, char delimiter, Cancel cancel)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var document = ExcelDocument.Load(stream);
        var sheets = new List<(string, string)>();
        foreach (var sheet in document.Sheets)
        {
            cancel.ThrowIfCancellationRequested();
            var table = sheet.ToDataTable(false);
            var records = table.Rows
                .Cast<DataRow>()
                .Select(_ => (IReadOnlyList<string>) [.. _.ItemArray.Select(Text)]);
            sheets.Add((sheet.Name, Delimited.Write(records, delimiter)));
        }

        return sheets;
    }

    // Invariant culture throughout: dates ISO 8601, numbers with a point to Excel's 15 significant digits
    // (750 * 1.0875 is stored as 815.6249999999999 and shown, and written, as 815.625).
    static string Text(object? value) =>
        value switch
        {
            null or DBNull => "",
            string text => text,
            bool flag => flag ? "true" : "false",
            DateTime date when date.TimeOfDay == TimeSpan.Zero => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime date => date.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            double number => number.ToString("G15", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };
}
