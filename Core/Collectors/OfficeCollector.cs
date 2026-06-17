using Microsoft.Win32;
using System;

namespace SoftAudit.Core.Collectors
{
    /// <summary>
    /// Collects Office-related information:
    /// - Version
    /// - License status
    /// </summary>
    public class OfficeCollector
    {
        public (string Version, string License) Collect()
        {
            string version = GetOfficeVersion();
            string license = new License.Checkers.OfficeChecker().CheckSystem();

            return (version, license);
        }

        // -------------------------
        // OFFICE VERSION
        // -------------------------
        private string GetOfficeVersion()
        {
            // =========================
            // 1. CLICK-TO-RUN (PRIORITY)
            // =========================
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");

                if (key != null)
                {
                    var product = key.GetValue("ProductReleaseIds")?.ToString();
                    var version = key.GetValue("ProductVersion")?.ToString();

                    if (!string.IsNullOrWhiteSpace(product))
                    {
                        // giữ raw + version
                        return $"Microsoft Office ({product}) [{version}]";
                    }
                }
            }
            catch
            {
            }

            // =========================
            // 2. FALLBACK: UNINSTALL
            // =========================
            var paths = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            try
            {
                foreach (var path in paths)
                {
                    using var key = Registry.LocalMachine.OpenSubKey(path);
                    if (key == null)
                        continue;

                    foreach (var subName in key.GetSubKeyNames())
                    {
                        using var sub = key.OpenSubKey(subName);
                        var name = sub?.GetValue("DisplayName")?.ToString();

                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        var lower = name.ToLowerInvariant();

                        if (lower.Contains("microsoft") && lower.Contains("office"))
                        {
                            return name.Trim(); // 🔥 giữ nguyên tên
                        }
                    }
                }
            }
            catch
            {
            }

            return "Not Detected";
        }
    }
}