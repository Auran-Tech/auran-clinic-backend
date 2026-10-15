using System.Text;

namespace Auran.Clinic.Infrastructure.Reporting;

/// <summary>
/// Protects user-controlled CSV values from being executed as spreadsheet formulas
/// when a clinic opens a downloaded report in Excel or similar software.
/// </summary>
public static class ReportCsvCell
{
    public static string Encode(string? value)
    {
        var text = value ?? string.Empty;
        var firstSignificant = 0;
        while (firstSignificant < text.Length &&
            (char.IsWhiteSpace(text[firstSignificant]) || char.IsControl(text[firstSignificant]) ||
             text[firstSignificant] == '\uFEFF'))
            firstSignificant++;

        if (firstSignificant < text.Length && "=+-@".Contains(text[firstSignificant]))
            text = "'" + text;

        // CSV structural quoting is separate from formula-injection prevention.
        if (!text.Contains(',') && !text.Contains('"') && !text.Contains('\r') && !text.Contains('\n'))
            return text;

        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
