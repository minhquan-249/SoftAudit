using System.Management;

namespace SoftAudit.Utils
{
    public static class SystemHelper
    {
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
    }
}
