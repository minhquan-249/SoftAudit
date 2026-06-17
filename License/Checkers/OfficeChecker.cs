using System.Diagnostics;
using System.IO;

namespace SoftAudit.License.Checkers
{
    /// <summary>
    /// Checks Microsoft Office activation status using OSPP.
    /// Supports Office 2016 / 2019 / 2021 / 2024 (key-based activation).
    /// </summary>
    public class OfficeChecker
    {
        /// <summary>
        /// Returns Office license status.
        /// Possible values:
        /// - Activated (Retail)
        /// - Activated (KMS)
        /// - Unknown
        /// </summary>
        public string CheckSystem()
        {
            try
            {
                var path = FindOspp();

                if (string.IsNullOrEmpty(path))
                    return "Unknown";

                var output = Run("cscript", $"\"{path}\" /dstatus");

                if (string.IsNullOrWhiteSpace(output))
                    return "Unknown";

                var lower = output.ToLowerInvariant();

                // -------------------------
                // LICENSED
                // -------------------------
                if (lower.Contains("licensed"))
                {
                    // ---- KMS ----
                    if (lower.Contains("kms"))
                        return "Activated (KMS)";

                    // ---- RETAIL / MAK ----
                    if (lower.Contains("retail") || lower.Contains("mak"))
                        return "Activated (Retail)";

                    // fallback
                    return "Activated";
                }

                // -------------------------
                // NOT LICENSED
                // -------------------------
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        // -------------------------
        // OSPP LOCATION
        // -------------------------
        private string FindOspp()
        {
            string[] paths =
            {
                @"C:\Program Files\Microsoft Office\Office16\ospp.vbs",
                @"C:\Program Files (x86)\Microsoft Office\Office16\ospp.vbs",
                @"C:\Program Files\Microsoft Office\root\Office16\ospp.vbs",
                @"C:\Program Files (x86)\Microsoft Office\root\Office16\ospp.vbs"
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            return "";
        }

        // -------------------------
        // PROCESS EXECUTION
        // -------------------------
        private string Run(string file, string args)
        {
            using var p = new Process();

            p.StartInfo.FileName = file;
            p.StartInfo.Arguments = args;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            p.Start();

            string output = p.StandardOutput.ReadToEnd();

            p.WaitForExit();

            return output.Trim();
        }
    }
}