using ClosedXML.Excel;
using SoftAudit.Core.Models;

namespace SoftAudit.Exporter
{
    /// <summary>
    /// One workbook per machine:
    /// - "Summary": one row (site, machine, network, Windows, Office) with a two-level header, as in v1.4.
    /// - "Software": installed software, one row per application, as in v1.3.
    /// HostName/IPv4 are repeated on every row so files from many machines can be stacked together.
    /// </summary>
    public static class ExcelExporter
    {
        public static void Export(AuditResult r, string filePath)
        {
            using var wb = new XLWorkbook();
            WriteSummary(wb.Worksheets.Add("Summary"), r);
            WriteSoftware(wb.Worksheets.Add("Software"), r);
            wb.SaveAs(filePath);
        }

        static void WriteSummary(IXLWorksheet ws, AuditResult r)
        {
            // row 1 = group, row 2 = column; single columns are merged over both rows
            string[] single = { "Site", "HostName", "Serial Number", "Machine Type", "IPv4", "MAC" };
            for (int c = 1; c <= single.Length; c++)
            {
                ws.Cell(1, c).Value = single[c - 1];
                ws.Range(1, c, 2, c).Merge();
            }

            Group(ws, 7, "Windows", "Version", "Activation", "Channel");
            Group(ws, 10, "Office", "Version", "Activation", "Channel");

            ws.Cell(1, 13).Value = "Check Date";
            ws.Range(1, 13, 2, 13).Merge();
            ws.Cell(1, 14).Value = "Tool Version";
            ws.Range(1, 14, 2, 14).Merge();

            object[] values =
            {
                r.Site, r.HostName, r.SerialNumber, r.MachineType, r.IPv4, r.MAC,
                r.WindowsVersion, r.WindowsActivation, r.WindowsChannel,
                r.OfficeVersion, r.OfficeActivation, r.OfficeChannel,
            };
            for (int c = 1; c <= values.Length; c++)
                ws.Cell(3, c).Value = values[c - 1]?.ToString() ?? "";

            ws.Cell(3, 13).Value = r.CheckDate;
            ws.Cell(3, 13).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
            ws.Cell(3, 14).Value = Program.ToolVersion;

            var header = ws.Range(1, 1, 2, 14);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            Highlight(ws.Cell(3, 8), r.WindowsActivation);
            Highlight(ws.Cell(3, 11), r.OfficeActivation);

            Finish(ws, 2);
        }

        static void WriteSoftware(IXLWorksheet ws, AuditResult r)
        {
            string[] headers = { "HostName", "IPv4", "Application", "Version", "Publisher", "Install Date" };
            for (int c = 1; c <= headers.Length; c++)
                ws.Cell(1, c).Value = headers[c - 1];

            var header = ws.Range(1, 1, 1, headers.Length);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var s in r.Software)
            {
                ws.Cell(row, 1).Value = r.HostName;
                ws.Cell(row, 2).Value = r.IPv4;
                ws.Cell(row, 3).Value = s.Name;
                ws.Cell(row, 4).Value = s.Version;
                ws.Cell(row, 5).Value = s.Publisher;
                ws.Cell(row, 6).Value = s.InstallDate == "" ? "N/A" : s.InstallDate;
                row++;
            }

            if (r.Software.Count > 0)
                ws.Range(1, 1, row - 1, headers.Length).SetAutoFilter();

            Finish(ws, 1);
        }

        static void Group(IXLWorksheet ws, int firstCol, string title, params string[] columns)
        {
            ws.Cell(1, firstCol).Value = title;
            ws.Range(1, firstCol, 1, firstCol + columns.Length - 1).Merge();
            for (int i = 0; i < columns.Length; i++)
                ws.Cell(2, firstCol + i).Value = columns[i];
        }

        /// <summary>Green when activated, red when not; leaves Unknown / "-" uncoloured.</summary>
        static void Highlight(IXLCell cell, string activation)
        {
            if (activation.StartsWith("Activated") || activation.StartsWith("Microsoft 365"))
                cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
            else if (activation.StartsWith("Not Activated") || activation.Contains("Grace"))
                cell.Style.Fill.BackgroundColor = XLColor.LightPink;
        }

        static void Finish(IXLWorksheet ws, int headerRows)
        {
            ws.SheetView.FreezeRows(headerRows);
            ws.Columns().AdjustToContents(1, 200);
            foreach (var col in ws.ColumnsUsed())
            {
                if (col.Width > 60) col.Width = 60;
                if (col.Width < 10) col.Width = 10;
            }

            var used = ws.RangeUsed();
            if (used != null)
            {
                used.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                used.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }
        }
    }
}
