using System;
using System.Management;

namespace SoftAudit.Utils
{
    public static class SystemHelper
    {
        // -------------------------
        // OS
        // -------------------------
        public static string GetOS()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Caption, Version FROM Win32_OperatingSystem");

                foreach (ManagementObject os in searcher.Get())
                {
                    string caption = os["Caption"]?.ToString() ?? "";
                    string version = os["Version"]?.ToString() ?? "";

                    return $"{caption} ({version})";
                }
            }
            catch
            {
            }

            return "Unknown OS";
        }

        // -------------------------
        // HOST NAME
        // -------------------------
        public static string GetHostName()
        {
            try
            {
                return Environment.MachineName;
            }
            catch
            {
                return "Unknown Host";
            }
        }

        // -------------------------
        // SERIAL NUMBER (BIOS)
        // -------------------------
        public static string GetSerialNumber()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT SerialNumber FROM Win32_BIOS");

                foreach (ManagementObject obj in searcher.Get())
                {
                    string serial = obj["SerialNumber"]?.ToString() ?? "";

                    if (!string.IsNullOrWhiteSpace(serial))
                        return serial.Trim();
                }
            }
            catch
            {
            }

            return "Unknown";
        }
    }
}
