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

        const string LocalFolder = @"C:\SoftAudit";

        /// <summary>
        /// SoftAudit.exe [/nopause]
        /// Site, Drive URL and share come from SoftAudit.ini next to the exe (see Utils/SiteConfig.cs).
        /// Exit code 0 = the result was saved somewhere, 1 = nothing saved.
        /// </summary>
        public static int Main(string[] args)
        {
            Console.OutputEncoding = new System.Text.UTF8Encoding(false);
            var iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SoftAudit.ini");
            var site = SiteConfig.Load(iniPath);
            Log.Site = site.SiteName;
            var started = DateTime.Now;

            Console.WriteLine($"SoftAudit v{ToolVersion}" + (site.SiteName == "" ? " (no SoftAudit.ini: local only)" : $" - {site.SiteName}"));
            Log.Info($"===== START ===== user {Environment.UserDomainName}\\{Environment.UserName}, exe {AppDomain.CurrentDomain.BaseDirectory}", console: false);
            Log.Info(File.Exists(iniPath)
                ? $"Config: {iniPath} | Drive: {(site.DriveUploadUrl == "" ? "off" : "on")} | Share: {(site.SharePath == "" ? "off" : site.SharePath)}"
                : $"Config: {iniPath} not found -> local only", console: false);
            Console.WriteLine("Starting audit...\n");

            var pause = !Array.Exists(args, a => a.Equals("/nopause", StringComparison.OrdinalIgnoreCase));

            int exitCode = 1;
            try
            {
                var result = Collect();
                result.Site = site.SiteName;
                exitCode = Save(result, site) ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FATAL] " + ex.Message);
                Log.Error("FATAL " + ex, console: false);
            }

            Log.Info($"===== END ===== exit code {exitCode}, {(DateTime.Now - started).TotalSeconds:0}s", console: false);
            if (Log.FilePath != null)
                Console.WriteLine($"\nLog: {Log.FilePath}");

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
            Log.Info($"[SYSTEM]   {r.HostName} | {r.MachineType} | {r.IPv4} | {r.MAC} | SN {r.SerialNumber}");

            r.WindowsVersion = SystemHelper.GetOS();
            var win = LicenseChecker.CheckWindows();
            r.WindowsActivation = win.Activation;
            r.WindowsChannel = win.Channel;
            Log.Info($"[WINDOWS]  {r.WindowsVersion} | {r.WindowsActivation} ({r.WindowsChannel})");

            var (officeVersion, office) = OfficeCollector.Collect();
            r.OfficeVersion = officeVersion;
            r.OfficeActivation = office.Activation;
            r.OfficeChannel = office.Channel;
            Log.Info($"[OFFICE]   {r.OfficeVersion} | {r.OfficeActivation} ({r.OfficeChannel})");

            r.Software = SoftwareScanner.Scan();
            Log.Info($"[SOFTWARE] {r.Software.Count} applications");

            return r;
        }

        /// <summary>
        /// Same tiers as SM-Tool: 1) always C:\SoftAudit, 2) Google Drive (site folder / SoftAudit),
        /// 3) site share only if Drive failed. Returns true when the file was saved anywhere.
        /// </summary>
        static bool Save(AuditResult r, SiteConfig site)
        {
            var fileName = $"{r.HostName}_{r.IPv4.Replace(":", "_")}_{r.CheckDate:yyyyMMdd}.xlsx";
            var localFile = Path.Combine(LocalFolder, fileName);

            try
            {
                Directory.CreateDirectory(LocalFolder);
                ExcelExporter.Export(r, localFile);
                Log.Info($"\n[EXPORT]   Local:   {localFile}");
            }
            catch (Exception ex)
            {
                // without a local copy there is nothing to upload; try writing straight to the share
                Log.Error($"\n[EXPORT]   Local failed: {ex.Message}");
                return site.SharePath != "" && TryShare(site.SharePath, fileName, dest => ExcelExporter.Export(r, dest));
            }

            if (site.DriveUploadUrl != "")
            {
                Log.Info("[EXPORT]   Uploading to Google Drive...");
                if (DriveUploader.Upload(site.DriveUploadUrl, site.SiteName, localFile))
                {
                    Log.Info($"[EXPORT]   Drive:   {site.SiteName}/{DriveUploader.SubFolder}/{fileName}");
                    return true;
                }
                Log.Warn("[EXPORT]   Drive failed.");
            }

            if (site.SharePath != "" && TryShare(site.SharePath, fileName, dest => File.Copy(localFile, dest, true)))
                return true;

            Log.Warn($"[EXPORT]   Only saved locally. Please send {localFile} to IT.");
            return true;
        }

        static bool TryShare(string sharePath, string fileName, Action<string> write)
        {
            try
            {
                // creates the SoftAudit subfolder on first run; throws if the share itself is unreachable
                Directory.CreateDirectory(sharePath);
                var dest = Path.Combine(sharePath, fileName);
                write(dest);
                Log.Info($"[EXPORT]   Share:   {dest}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"[EXPORT]   Share failed: {ex.Message}");
                return false;
            }
        }
    }
}
