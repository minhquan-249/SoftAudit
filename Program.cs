using System;
using System.IO;
using SoftAudit.Core;
using SoftAudit.Core.Collectors;
using SoftAudit.Core.Models;
using SoftAudit.Exporter;
using SoftAudit.License;
using SoftAudit.Utils;

namespace SoftAudit
{
    public static class Program
    {
        public const string ToolVersion = "2.0.0";

        const string DefaultSharePath = @"\\10.168.109.254\Software\1. Tool Soft Audit\Data";
        const string LocalFolder = @"C:\SoftAudit";

        /// <summary>
        /// SoftAudit.exe [shareFolder] [/nopause]
        /// shareFolder overrides the default network folder (e.g. for another site).
        /// Exit code 0 = at least the local copy was saved, 1 = nothing saved.
        /// </summary>
        public static int Main(string[] args)
        {
            Console.OutputEncoding = new System.Text.UTF8Encoding(false);
            Console.WriteLine($"SoftAudit v{ToolVersion}");
            Console.WriteLine("Starting audit...\n");

            var sharePath = DefaultSharePath;
            var pause = true;
            foreach (var a in args)
            {
                if (a.Equals("/nopause", StringComparison.OrdinalIgnoreCase)) pause = false;
                else sharePath = a;
            }

            int exitCode = 1;
            try
            {
                var result = Collect();
                exitCode = Save(result, sharePath) ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FATAL] " + ex.Message);
            }

            if (pause && !Console.IsInputRedirected)
            {
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey(true);
            }
            return exitCode;
        }

        static AuditResult Collect()
        {
            var r = new AuditResult { HostName = SystemHelper.GetHostName() };

            var isServer = SystemHelper.IsServer();
            r.MachineType = isServer ? "Server" : "Client";
            r.SerialNumber = SystemHelper.GetSerialNumber();
            r.IPv4 = NetworkHelper.GetActiveIP(isServer);
            r.MAC = NetworkHelper.GetMac(r.IPv4);
            Console.WriteLine($"[SYSTEM]   {r.HostName} | {r.MachineType} | {r.IPv4} | {r.MAC} | SN {r.SerialNumber}");

            r.WindowsVersion = SystemHelper.GetOS();
            var win = LicenseChecker.CheckWindows();
            r.WindowsActivation = win.Activation;
            r.WindowsChannel = win.Channel;
            Console.WriteLine($"[WINDOWS]  {r.WindowsVersion} | {r.WindowsActivation} ({r.WindowsChannel})");

            var (officeVersion, office) = OfficeCollector.Collect();
            r.OfficeVersion = officeVersion;
            r.OfficeActivation = office.Activation;
            r.OfficeChannel = office.Channel;
            Console.WriteLine($"[OFFICE]   {r.OfficeVersion} | {r.OfficeActivation} ({r.OfficeChannel})");

            r.Software = SoftwareScanner.Scan();
            Console.WriteLine($"[SOFTWARE] {r.Software.Count} applications");

            return r;
        }

        /// <summary>Always writes C:\SoftAudit first (nothing is lost if the share is down), then copies to the share.</summary>
        static bool Save(AuditResult r, string sharePath)
        {
            var fileName = $"{r.HostName}_{r.IPv4.Replace(":", "_")}_{r.CheckDate:yyyyMMdd}.xlsx";
            var localFile = Path.Combine(LocalFolder, fileName);
            var shareFile = Path.Combine(sharePath, fileName);
            bool localOk = false, shareOk = false;

            try
            {
                Directory.CreateDirectory(LocalFolder);
                ExcelExporter.Export(r, localFile);
                localOk = true;
                Console.WriteLine($"\n[EXPORT]   Local:   {localFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[EXPORT]   Local failed: {ex.Message}");
            }

            try
            {
                if (!Directory.Exists(sharePath))
                    throw new IOException("share not reachable");
                if (localOk)
                    File.Copy(localFile, shareFile, overwrite: true);
                else
                    ExcelExporter.Export(r, shareFile);
                shareOk = true;
                Console.WriteLine($"[EXPORT]   Network: {shareFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXPORT]   Network failed: {ex.Message}");
                if (localOk)
                    Console.WriteLine($"           Please send {localFile} to IT.");
            }
            return localOk || shareOk;
        }
    }
}
