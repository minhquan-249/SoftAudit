using ClosedXML.Excel;
using SoftAudit.Core.Models;
using System.Linq;

namespace SoftAudit.Exporter
{
    /// <summary>
    /// Exports AuditResult to Excel format.
    /// </summary>
    public class ExcelExporter
    {
        public void Export(AuditResult data, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Audit Result");

            // -------------------------
            // HEADER ROW 1 (GROUP)
            // -------------------------
            worksheet.Cell(1, 1).Value = "HostName";
            worksheet.Cell(1, 2).Value = "SerialNumber";
            worksheet.Cell(1, 3).Value = "IPv4";

            worksheet.Cell(1, 4).Value = "Windows";
            worksheet.Range(1, 4, 1, 6).Merge();

            worksheet.Cell(1, 7).Value = "Office";
            worksheet.Range(1, 7, 1, 8).Merge();
            // -------------------------
            // HEADER ROW 2 (DETAIL)
            // -------------------------
            worksheet.Cell(2, 4).Value = "Version";
            worksheet.Cell(2, 5).Value = "Activation";
            worksheet.Cell(2, 6).Value = "Type";

            worksheet.Cell(2, 7).Value = "Version";
            worksheet.Cell(2, 8).Value = "Activation";

            // merge static columns (vertical)
            worksheet.Range(1, 1, 2, 1).Merge();
            worksheet.Range(1, 2, 2, 2).Merge();
            worksheet.Range(1, 3, 2, 3).Merge();

            // -------------------------
            // HEADER STYLE
            // -------------------------

            var headerRange = worksheet.Range(1, 1, 2, 8);

            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // -------------------------
            // DATA
            // -------------------------
            int col = 1;
            int row = 3;

            worksheet.Cell(row, col++).Value = data.HostName;
            worksheet.Cell(row, col++).Value = data.SerialNumber;
            worksheet.Cell(row, col++).Value = data.IPv4;

            worksheet.Cell(row, col++).Value = data.WindowsVersion;
            worksheet.Cell(row, col++).Value = data.WindowsLicense;
            worksheet.Cell(row, col++).Value = data.WindowsLicenseType;

            worksheet.Cell(row, col++).Value = data.OfficeVersion;
            worksheet.Cell(row, col++).Value = data.OfficeLicense;

            // -------------------------
            // FORMAT
            // -------------------------
            worksheet.Column(1).Width = 20; // HostName
            worksheet.Column(2).Width = 20; // SerialNumber
            worksheet.Column(3).Width = 18; // IPv4

            worksheet.Column(4).Width = 40; // Windows Version
            worksheet.Column(5).Width = 15; // Activation
            worksheet.Column(6).Width = 12; // Type

            worksheet.Column(7).Width = 40; // Office Version
            worksheet.Column(8).Width = 18; // Activation


            // freeze 2 header rows
            worksheet.SheetView.FreezeRows(2);

            var usedRange = worksheet.RangeUsed();
            if (usedRange != null)
            {
                usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            workbook.SaveAs(filePath);
        }
    }
}