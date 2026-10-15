using System.IO.Compression;
using System.Xml.Linq;
using Auran.Clinic.Application.Reporting;
using Auran.Clinic.Infrastructure.Reporting;

namespace Auran.Clinic.UnitTests.Reporting;

public sealed class VisitReportXlsxWriterTests
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    [Fact]
    public void Empty_report_is_valid_opc_workbook_with_headers()
    {
        var bytes = VisitReportXlsxWriter.Create(new VisitReportResponse(null, null, 0, []));
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
        Assert.NotNull(zip.GetEntry("_rels/.rels"));
        Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
        Assert.NotNull(zip.GetEntry("xl/styles.xml"));
        Assert.NotNull(zip.GetEntry("xl/_rels/workbook.xml.rels"));
        var sheet = ReadXml(zip, "xl/worksheets/sheet1.xml");
        var rows = sheet.Descendants(Spreadsheet + "row").ToList();
        Assert.Single(rows);
        Assert.Equal(11, rows[0].Elements(Spreadsheet + "c").Count());
    }

    [Fact]
    public void Clinical_text_remains_inline_not_executable_formula_and_dates_are_numbers()
    {
        var entryUtc = new DateTime(2026, 10, 15, 10, 30, 0, DateTimeKind.Utc);
        var patientName = "=HYPERLINK(\"https://example.invalid\",\"unsafe\")";
        var report = new VisitReportResponse(
            null, null, 1,
            [new VisitReportRowResponse(
                Guid.NewGuid(), Guid.NewGuid(), "00123", patientName,
                Guid.NewGuid(), "Arabic أحمد & <test>", "Open", "Draft",
                entryUtc, null, null, "+SUM(1,1)", "@cmd")]);

        using var zip = new ZipArchive(new MemoryStream(VisitReportXlsxWriter.Create(report)), ZipArchiveMode.Read);
        var sheet = ReadXml(zip, "xl/worksheets/sheet1.xml");
        var cells = sheet.Descendants(Spreadsheet + "row").Skip(1)
            .Single().Elements(Spreadsheet + "c").ToList();
        Assert.Equal(11, cells.Count);
        Assert.Equal("inlineStr", (string?)cells[2].Attribute("t"));
        Assert.Equal(patientName, cells[2].Descendants(Spreadsheet + "t").Single().Value);
        Assert.Equal("00123", cells[1].Descendants(Spreadsheet + "t").Single().Value);
        Assert.Equal("+SUM(1,1)", cells[9].Descendants(Spreadsheet + "t").Single().Value);
        Assert.Equal("@cmd", cells[10].Descendants(Spreadsheet + "t").Single().Value);
        Assert.Empty(sheet.Descendants(Spreadsheet + "f"));
        Assert.Equal("1", (string?)cells[6].Attribute("s"));
        Assert.Equal(entryUtc.ToOADate().ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            cells[6].Element(Spreadsheet + "v")?.Value);
        Assert.Equal("Arabic أحمد & <test>", cells[3].Descendants(Spreadsheet + "t").Single().Value);
    }

    private static XDocument ReadXml(ZipArchive zip, string name)
    {
        using var entryStream = zip.GetEntry(name)!.Open();
        return XDocument.Load(entryStream);
    }
}
