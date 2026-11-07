using System;
using System.Management;

namespace SoftAudit.Utils
{
    public static class SystemHelper
    {
        public static string GetHostName() => Environment.MachineName;

        /// <summary>"Windows Server 2019 Standard (10.0.17763)".</summary>
        public static string GetOS()
        {
            var caption = Wmi("Win32_OperatingSystem", "Caption");
            var version = Wmi("Win32_OperatingSystem", "Version");
            return caption == "" ? "Unknown OS" : $"{caption.Trim()} ({version})";
        }

        /// <summary>
        /// Win32_OperatingSystem.ProductType: 1 = workstation, 2 = domain controller, 3 = server.
        /// </summary>
        public static bool IsServer() => Wmi("Win32_OperatingSystem", "ProductType") is "2" or "3";

        public static string GetSerialNumber()
        {
            var serial = Wmi("Win32_BIOS", "SerialNumber").Trim();
            return serial == "" ? "Unknown" : serial;
        }

        /// <summary>First value of one WMI property, "" when missing or on error.</summary>
        public static string Wmi(string wmiClass, string property)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
                foreach (ManagementObject obj in searcher.Get())
                    using (obj)
                        return obj[property]?.ToString() ?? "";
            }
            catch
            {
            }
            return "";
        }
    }
}
