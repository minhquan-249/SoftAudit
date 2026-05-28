using ClosedXML.Excel;
using SoftAudit.Core.Models;
using System;
using System.Collections.Generic;

namespace SoftAudit.Exporter
{
    public class ExcelExporter
    {
        public void Export(List<Software> data, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Software Inventory");

            // headers
            string[] headers =
            {
                "HostName",
                "IPv4",
                "OS",
                "Application",
                "Version",
                "Publisher",
                "Install Date",
                "License Status"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            // header style
            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // data
            int row = 2;

            foreach (var s in data)
            {
                worksheet.Cell(row, 1).Value = s.HostName;
                worksheet.Cell(row, 2).Value = s.IPv4;
                worksheet.Cell(row, 3).Value = s.OS;
                worksheet.Cell(row, 4).Value = s.Name;
                worksheet.Cell(row, 5).Value = s.Version;
                worksheet.Cell(row, 6).Value = s.Publisher;
                worksheet.Cell(row, 7).Value = FormatDate(s.InstallDate);
                worksheet.Cell(row, 8).Value = string.IsNullOrEmpty(s.LicenseStatus) ? "N/A" : s.LicenseStatus;

                row++;
            }

            // adjust column width
            worksheet.Columns().AdjustToContents();

            // freeze header
            worksheet.SheetView.FreezeRows(1);

            // border
            var usedRange = worksheet.RangeUsed();
            if (usedRange != null)
            {
                usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            workbook.SaveAs(filePath);
        }

        private string FormatDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate))
                return "N/A";

            if (rawDate.Length == 8)
            {
                try
                {
                    var date = DateTime.ParseExact(rawDate, "yyyyMMdd", null);
                    return date.ToString("yyyy-MM-dd");
                }
                catch
                {
                }
            }

            return rawDate;
        }
    }
}