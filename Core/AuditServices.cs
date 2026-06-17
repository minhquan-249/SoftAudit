using SoftAudit.Core.Collectors;
using SoftAudit.Core.Models;
using SoftAudit.Utils;

namespace SoftAudit.Core
{
    /// <summary>
    /// Coordinates all collectors and builds final audit result.
    /// </summary>
    public class AuditService
    {
        private readonly WindowsCollector _windows;
        private readonly OfficeCollector _office;

        public AuditService()
        {
            _windows = new WindowsCollector();
            _office = new OfficeCollector();
        }

        /// <summary>
        /// Runs full audit process and returns a single result object.
        /// </summary>
        public AuditResult Run()
        {
            var result = new AuditResult();

            // -------------------------
            // SYSTEM
            // -------------------------
            result.HostName = SystemHelper.GetHostName();
            result.SerialNumber = SystemHelper.GetSerialNumber();
            result.IPv4 = NetworkHelper.GetActiveIP();

            // -------------------------
            // WINDOWS
            // -------------------------
            var windows = _windows.Collect();

            result.WindowsVersion = windows.Version;

            result.WindowsLicense = windows.License.Status;
            result.WindowsLicenseType = windows.License.Type;

            // -------------------------
            // OFFICE
            // -------------------------
            var office = _office.Collect();

            result.OfficeVersion = office.Version;
            result.OfficeLicense = office.License;

            return result;
        }
    }
}
