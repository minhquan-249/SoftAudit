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
            // 1. ƯU TIÊN: UNINSTALL (FULL NAME CHUẨN)
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
                        var publisher = sub?.GetValue("Publisher")?.ToString();

                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        var lower = name.ToLowerInvariant();

                        // ✅ lọc chính xác hơn để tránh dính Office Tools / OneDrive...
                        if (lower.Contains("microsoft office") &&
                            !lower.Contains("update") &&
                            !lower.Contains("click-to-run"))
                        {
                            return name.Trim(); // 🔥 chính là thứ bạn cần
                        }
                    }
                }
            }
            catch
            {
            }

            // =========================
            // 2. FALLBACK: CLICK-TO-RUN (KHÔNG ĐẸP NHƯNG CỨU CÁNH)
            // =========================
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");

                if (key != null)
                {
                    var client = key.GetValue("ClientVersionToReport")?.ToString();
                    var product = key.GetValue("ProductReleaseIds")?.ToString();

                    if (!string.IsNullOrWhiteSpace(product))
                    {
                        return $"Microsoft Office ({product}) [{client}]";
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