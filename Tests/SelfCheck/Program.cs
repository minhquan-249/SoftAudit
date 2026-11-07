using System;
using System.IO;
using ClosedXML.Excel;
using SoftAudit.Core;
using SoftAudit.Core.Models;
using SoftAudit.Exporter;
using SoftAudit.License;
using SoftAudit.Utils;

int failed = 0;
void Check(bool ok, string what) { Console.WriteLine((ok ? "PASS " : "FAIL ") + what); if (!ok) failed++; }

// name rules (ported from software_rules.json)
Check(SoftwareScanner.Normalize("Microsoft Visual Studio Code (User)") == "Visual Studio Code", "VS Code mapping");
Check(SoftwareScanner.Normalize("7-Zip 23.01 (x64)") == "7-Zip 23.01", "strip (x64)");
Check(!SoftwareScanner.Keep("Security Update for Microsoft Office (KB5002)"), "exclude KB");
Check(!SoftwareScanner.Keep("Microsoft Office Proofing Tools"), "exclude proofing");
Check(SoftwareScanner.Keep("Microsoft Edge WebView2 Runtime"), "webview2 always kept");
Check(SoftwareScanner.Keep("Google Chrome"), "keep normal app");
Check(SoftwareScanner.FormatDate("20250131") == "2025-01-31", "install date format");
Check(SoftwareScanner.FormatDate("") == "", "empty install date");

// licence text (WMI Description / LicenseStatus)
Check(LicenseChecker.ChannelText("Windows(R) Operating System, VOLUME_KMSCLIENT channel") == "KMS", "KMS channel");
Check(LicenseChecker.ChannelText("Windows(R) Operating System, OEM_DM channel") == "OEM", "OEM channel");
Check(LicenseChecker.ChannelText("Windows(R) Operating System, RETAIL channel") == "Retail", "Retail channel");
Check(LicenseChecker.ChannelText("Office 19, VOLUME_MAK channel") == "MAK", "MAK channel");
Check(LicenseChecker.StatusText(1) == "Activated", "status 1");
Check(LicenseChecker.StatusText(0) == "Not Activated", "status 0 (v1.x bug: 'UNLICENSED' read as licensed)");

// Excel layout
var r = new AuditResult
{
    Site = "LITV", HostName = "PC01", SerialNumber = "SN1", MachineType = "Server", IPv4 = "10.0.0.5", MAC = "AA-BB-CC-DD-EE-FF",
    WindowsVersion = "Windows Server 2019 Standard (10.0.17763)", WindowsActivation = "Activated", WindowsChannel = "KMS",
    OfficeVersion = "Not Detected", OfficeActivation = "-", OfficeChannel = "-",
};
r.Software.Add(new Software { Name = "Google Chrome", Version = "130", Publisher = "Google LLC", InstallDate = "2025-01-31" });
r.Software.Add(new Software { Name = "7-Zip", Version = "23.01" });
var path = Path.Combine(Path.GetTempPath(), "selfcheck.xlsx");
ExcelExporter.Export(r, path);
using (var wb = new XLWorkbook(path))
{
    var s = wb.Worksheet("Summary");
    Check(s.Cell(1, 7).GetString() == "Windows" && s.Cell(2, 8).GetString() == "Activation", "summary header groups");
    Check(s.Cell(3, 1).GetString() == "LITV" && s.Cell(3, 4).GetString() == "Server" && s.Cell(3, 6).GetString() == "AA-BB-CC-DD-EE-FF", "summary site + type + MAC");
    Check(s.Cell(3, 9).GetString() == "KMS", "summary windows channel");
    var sw = wb.Worksheet("Software");
    Check(sw.Cell(2, 3).GetString() == "Google Chrome" && sw.Cell(3, 6).GetString() == "N/A", "software rows");
    Check(sw.Cell(3, 1).GetString() == "PC01", "hostname repeated on software rows");
    Check(s.Column(7).Width == 42 && sw.Column(3).Width == 50, "fixed column widths");
}

// site config (SoftAudit.ini)
var ini = Path.Combine(Path.GetTempPath(), "SoftAudit.ini");
File.WriteAllLines(ini, new[] { "; comment", "SiteName = HOT", "DriveUploadUrl=\"https://x/exec\"", @"SharePath=\\10.0.0.1\Data\SoftAudit", "Unknown=1" });
var cfg = SiteConfig.Load(ini);
Check(cfg.SiteName == "HOT" && cfg.DriveUploadUrl == "https://x/exec" && cfg.SharePath == @"\\10.0.0.1\Data\SoftAudit", "ini parse");
Check(SiteConfig.Load(ini + ".missing").SiteName == "", "missing ini = no site");

// Drive form + response (Apps Script contract: site, subFolder, fileName, fileData base64)
var form = DriveUploader.BuildForm("HOT", "PC 01.xlsx", new byte[] { 0xFF, 0xFE, 0x00 });
Check(form == "site=HOT&subFolder=SoftAudit&fileName=PC%2001.xlsx&fileData=%2F%2F4A", "drive form encoding");
Check(DriveUploader.IsSuccess("{\"success\": true, \"fileId\":\"1\"}") && !DriveUploader.IsSuccess("{\"success\":false}"), "drive response parse");

Console.WriteLine(failed == 0 ? "\nALL PASS" : $"\n{failed} FAILED");
return failed == 0 ? 0 : 1;
