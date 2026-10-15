using Auran.Clinic.Infrastructure.Reporting;

namespace Auran.Clinic.UnitTests.Reporting;

public sealed class ReportCsvCellTests
{
    [Theory]
    [InlineData("=HYPERLINK(\"https://invalid.example\",\"click\")")]
    [InlineData("+SUM(1,2)")]
    [InlineData("-1+2")]
    [InlineData("@SUM(A1:A2)")]
    [InlineData("  =SUM(A1:A2)")]
    [InlineData("\t@cmd")]
    [InlineData("\uFEFF=2+2")]
    public void Formula_like_user_strings_are_prefix_neutralized(string value)
    {
        var encoded = ReportCsvCell.Encode(value);
        Assert.StartsWith("'", encoded);
        Assert.Contains(value[0].ToString(), encoded);
    }

    [Theory]
    [InlineData("Regular clinic note", "Regular clinic note")]
    [InlineData("00123", "00123")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("quoted \"text\"", "\"quoted \"\"text\"\"\"")]
    public void Benign_csv_values_preserve_standard_quoting(string input, string expected)
    {
        Assert.Equal(expected, ReportCsvCell.Encode(input));
    }

    [Fact]
    public void Empty_and_null_cells_are_blank()
    {
        Assert.Equal(string.Empty, ReportCsvCell.Encode(null));
        Assert.Equal(string.Empty, ReportCsvCell.Encode(""));
    }
}
