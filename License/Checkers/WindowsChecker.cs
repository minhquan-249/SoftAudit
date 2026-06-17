using System.Diagnostics;
using SoftAudit.Core.Models;

namespace SoftAudit.License.Checkers
{
    /// <summary>
    /// Checks Windows activation information using slmgr.
    /// Supports Retail / OEM / KMS activation models.
    /// </summary>
    public class WindowsChecker
    {
        /// <summary>
        /// Returns Windows license details:
        /// - Status: Activated / Activated (KMS) / Not Activated / Unknown
        /// - Type: Retail / KMS / Unknown
        /// </summary>
        public WindowsLicenseResult CheckSystem()
        {
            try
            {
                var result = new WindowsLicenseResult();

                var output = Run("cscript", @"//NoLogo C:\Windows\System32\slmgr.vbs /dlv");

                if (string.IsNullOrWhiteSpace(output))
                {
                    result.Status = "Unknown";
                    result.Type = "Unknown";
                    return result;
                }

                var lower = output.ToLowerInvariant();

                // -------------------------
                // LICENSE STATUS
                // -------------------------
                if (lower.Contains("license status: licensed"))
                {
                    if (lower.Contains("kms"))
                    {
                        result.Status = "Activated (KMS)";
                        result.Type = "KMS";
                    }
                    else
                    {
                        result.Status = "Activated";
                        result.Type = "Retail";
                    }
                }
                else
                {
                    result.Status = "Not Activated";
                    result.Type = "Unknown";
                }

                return result;
            }
            catch
            {
                return new WindowsLicenseResult
                {
                    Status = "Unknown",
                    Type = "Unknown",
                };
            }
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