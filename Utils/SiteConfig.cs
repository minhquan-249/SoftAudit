using System;
using System.IO;

namespace SoftAudit.Utils
{
    /// <summary>
    /// SoftAudit.ini next to the exe (copied from Sites\&lt;Site&gt;.ini by Build.ps1), same keys as SM-Tool's Sites\*.psd1:
    ///   SiteName=LITV
    ///   DriveUploadUrl=https://script.google.com/macros/s/.../exec   (empty = skip Drive)
    ///   SharePath=\\server\share\...\SoftAudit                          (empty = skip share)
    /// Missing file = no site, results saved to C:\SoftAudit only.
    /// </summary>
    public class SiteConfig
    {
        public string SiteName { get; set; } = "";
        public string DriveUploadUrl { get; set; } = "";
        public string SharePath { get; set; } = "";

        public static SiteConfig Load(string path)
        {
            var cfg = new SiteConfig();
            if (!File.Exists(path))
                return cfg;

            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line == "" || line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                var eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;

                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim().Trim('"', '\'');

                if (key.Equals("SiteName", StringComparison.OrdinalIgnoreCase)) cfg.SiteName = value;
                else if (key.Equals("DriveUploadUrl", StringComparison.OrdinalIgnoreCase)) cfg.DriveUploadUrl = value;
                else if (key.Equals("SharePath", StringComparison.OrdinalIgnoreCase)) cfg.SharePath = value;
            }
            return cfg;
        }
    }
}
