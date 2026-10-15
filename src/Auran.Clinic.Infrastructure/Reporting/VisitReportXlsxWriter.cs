using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using Auran.Clinic.Application.Reporting;

namespace Auran.Clinic.Infrastructure.Reporting;

/// <summary>
/// Minimal, dependency-free OpenXML workbook for tenant-scoped visit exports.
/// User-controlled strings are always inline strings, never Excel formulas.
/// </summary>
public static class VisitReportXlsxWriter
{
    private const string SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static readonly string[] Headers =
    [
        "Visit ID", "Patient Number", "Patient Name", "Doctor Name",
        "Visit Status", "Documentation Status", "Entry (UTC)",
        "Completed (UTC)", "Exit (UTC)", "Diagnosis", "Treatment Plan"
    ];

    public static byte[] Create(VisitReportResponse report)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteXml(archive, "[Content_Types].xml", writer =>
            {
                writer.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
                writer.WriteStartElement("Default");
                writer.WriteAttributeString("Extension", "rels");
                writer.WriteAttributeString("ContentType", "application/vnd.openxmlformats-package.relationships+xml");
                writer.WriteEndElement();
                writer.WriteStartElement("Default");
                writer.WriteAttributeString("Extension", "xml");
                writer.WriteAttributeString("ContentType", "application/xml");
                writer.WriteEndElement();
                Override(writer, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
                Override(writer, "/xl/worksheets/sheet1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
                Override(writer, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
                writer.WriteEndElement();
            });

            WriteXml(archive, "_rels/.rels", writer =>
            {
                writer.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
                Relationship(writer, "rId1", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument", "xl/workbook.xml");
                writer.WriteEndElement();
            });

            WriteXml(archive, "xl/workbook.xml", writer =>
            {
                writer.WriteStartElement("workbook", SpreadsheetNs);
                writer.WriteAttributeString("xmlns", "r", null, RelationshipNs);
                writer.WriteStartElement("sheets", SpreadsheetNs);
                writer.WriteStartElement("sheet", SpreadsheetNs);
                writer.WriteAttributeString("name", "Visits");
                writer.WriteAttributeString("sheetId", "1");
                writer.WriteAttributeString("r", "id", RelationshipNs, "rId1");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            });

            WriteXml(archive, "xl/_rels/workbook.xml.rels", writer =>
            {
                writer.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
                Relationship(writer, "rId1", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet", "worksheets/sheet1.xml");
                Relationship(writer, "rId2", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles", "styles.xml");
                writer.WriteEndElement();
            });

            WriteXml(archive, "xl/styles.xml", writer =>
            {
                writer.WriteStartElement("styleSheet", SpreadsheetNs);
                writer.WriteStartElement("numFmts", SpreadsheetNs);
                writer.WriteAttributeString("count", "1");
                writer.WriteStartElement("numFmt", SpreadsheetNs);
                writer.WriteAttributeString("numFmtId", "164");
                writer.WriteAttributeString("formatCode", "yyyy-mm-dd hh:mm");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("fonts", SpreadsheetNs);
                writer.WriteAttributeString("count", "1");
                writer.WriteStartElement("font", SpreadsheetNs);
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("fills", SpreadsheetNs);
                writer.WriteAttributeString("count", "1");
                writer.WriteStartElement("fill", SpreadsheetNs);
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("borders", SpreadsheetNs);
                writer.WriteAttributeString("count", "1");
                writer.WriteStartElement("border", SpreadsheetNs);
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("cellStyleXfs", SpreadsheetNs);
                writer.WriteAttributeString("count", "1");
                WriteXf(writer, 0);
                writer.WriteEndElement();
                writer.WriteStartElement("cellXfs", SpreadsheetNs);
                writer.WriteAttributeString("count", "2");
                WriteXf(writer, 0);
                WriteXf(writer, 164);
                writer.WriteEndElement();
                writer.WriteEndElement();
            });

            WriteXml(archive, "xl/worksheets/sheet1.xml", writer =>
            {
                writer.WriteStartElement("worksheet", SpreadsheetNs);
                writer.WriteStartElement("sheetData", SpreadsheetNs);
                writer.WriteStartElement("row", SpreadsheetNs);
                writer.WriteAttributeString("r", "1");
                foreach (var heading in Headers)
                    TextCell(writer, heading);
                writer.WriteEndElement();

                var rowNumber = 2;
                foreach (var row in report.Rows)
                {
                    writer.WriteStartElement("row", SpreadsheetNs);
                    writer.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));
                    TextCell(writer, row.VisitId.ToString());
                    TextCell(writer, row.PatientNumber);
                    TextCell(writer, row.PatientName);
                    TextCell(writer, row.DoctorName);
                    TextCell(writer, row.VisitStatus);
                    TextCell(writer, row.DocumentationStatus);
                    DateCell(writer, row.EntryAtUtc);
                    DateCell(writer, row.CompletedAtUtc);
                    DateCell(writer, row.ExitAtUtc);
                    TextCell(writer, row.Diagnosis);
                    TextCell(writer, row.TreatmentPlan);
                    writer.WriteEndElement();
                    rowNumber++;
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
            });
        }
        return stream.ToArray();
    }

    private static void TextCell(XmlWriter writer, string? text)
    {
        // Inline text cannot execute =SUM(), +, -, @, or DDE spreadsheet formulas.
        writer.WriteStartElement("c", SpreadsheetNs);
        writer.WriteAttributeString("t", "inlineStr");
        writer.WriteStartElement("is", SpreadsheetNs);
        writer.WriteStartElement("t", SpreadsheetNs);
        writer.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
        writer.WriteString(SanitizeXml(text));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void DateCell(XmlWriter writer, DateTime? date)
    {
        if (!date.HasValue) { TextCell(writer, null); return; }
        writer.WriteStartElement("c", SpreadsheetNs);
        writer.WriteAttributeString("s", "1");
        writer.WriteStartElement("v", SpreadsheetNs);
        writer.WriteString(date.Value.ToOADate().ToString("R", CultureInfo.InvariantCulture));
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static string SanitizeXml(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
            if (XmlConvert.IsXmlChar(ch)) builder.Append(ch);
        return builder.ToString();
    }

    private static void WriteXml(ZipArchive zip, string path, Action<XmlWriter> write)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
        using var entryStream = entry.Open();
        using var writer = XmlWriter.Create(entryStream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            CloseOutput = false
        });
        writer.WriteStartDocument();
        write(writer);
        writer.WriteEndDocument();
    }

    private static void Override(XmlWriter writer, string name, string type)
    {
        writer.WriteStartElement("Override");
        writer.WriteAttributeString("PartName", name);
        writer.WriteAttributeString("ContentType", type);
        writer.WriteEndElement();
    }

    private static void Relationship(XmlWriter writer, string id, string type, string target)
    {
        writer.WriteStartElement("Relationship");
        writer.WriteAttributeString("Id", id);
        writer.WriteAttributeString("Type", type);
        writer.WriteAttributeString("Target", target);
        writer.WriteEndElement();
    }

    private static void WriteXf(XmlWriter writer, int numberFormatId)
    {
        writer.WriteStartElement("xf", SpreadsheetNs);
        writer.WriteAttributeString("numFmtId", numberFormatId.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("fontId", "0");
        writer.WriteAttributeString("fillId", "0");
        writer.WriteAttributeString("borderId", "0");
        writer.WriteAttributeString("xfId", "0");
        if (numberFormatId != 0) writer.WriteAttributeString("applyNumberFormat", "1");
        writer.WriteEndElement();
    }
}
