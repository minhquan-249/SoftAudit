namespace SoftAudit.Core.Models
{
    public class Software
    {
        public string HostName {get; set; } = "";
        public string IPv4 {get; set; } = "";
        public string OS {get; set; } = "";
        // Normalized name (sau khi clean)
        public string Name { get; set; } = "";

        // Original name (raw từ registry)
        public string RawName { get; set; } = "";

        public string Version { get; set; } = "";
        public string Publisher { get; set; } = "";

        public string InstallDate { get; set; } = "";
        // License
        public string LicenseStatus { get; set; } = "";
    }
}