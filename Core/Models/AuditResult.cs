namespace SoftAudit.Core.Models
{
    /// <summary>
    /// Represents audit data for a single machine.
    /// Designed for export and downstream analysis.
    /// </summary>
    public class AuditResult
    {
        // -------------------------
        // SYSTEM
        // -------------------------
        public string HostName { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string IPv4 { get; set; } = "";

        // -------------------------
        // WINDOWS
        // -------------------------
        public string WindowsVersion { get; set; } = "";

        /// <summary>
        /// Activation status (Activated / Activated (KMS) / Not Activated / Unknown).
        /// </summary>
        public string WindowsLicense { get; set; } = "";

        /// <summary>
        /// License type (Retail / KMS / Unknown).
        /// </summary>
        public string WindowsLicenseType { get; set; } = "";

        /// <summary>
        /// Last 5 characters of product key for identification.
        /// </summary>
        // public string WindowsPartialKey { get; set; } = "";

        // -------------------------
        // OFFICE
        // -------------------------
        public string OfficeVersion { get; set; } = "";
        public string OfficeLicense { get; set; } = "";

    }
}