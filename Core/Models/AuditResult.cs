using System;
using System.Collections.Generic;

namespace SoftAudit.Core.Models
{
    /// <summary>Everything collected from one machine.</summary>
    public class AuditResult
    {
        public string HostName { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string MachineType { get; set; } = "";   // Server / Client
        public string IPv4 { get; set; } = "";
        public string MAC { get; set; } = "";

        public string WindowsVersion { get; set; } = "";
        public string WindowsActivation { get; set; } = "";
        public string WindowsChannel { get; set; } = "";

        public string OfficeVersion { get; set; } = "";
        public string OfficeActivation { get; set; } = "";
        public string OfficeChannel { get; set; } = "";

        public DateTime CheckDate { get; set; } = DateTime.Now;

        public List<Software> Software { get; set; } = new List<Software>();
    }

    public class Software
    {
        public string RawName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Publisher { get; set; } = "";
        public string InstallDate { get; set; } = "";
    }

    /// <summary>Activation state of one product (Windows or Office).</summary>
    public class LicenseInfo
    {
        public string Activation { get; set; } = "Unknown";
        public string Channel { get; set; } = "Unknown";
    }
}
