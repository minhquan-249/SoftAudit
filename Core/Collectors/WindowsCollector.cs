using SoftAudit.License.Checkers;
using SoftAudit.Core.Models;

namespace SoftAudit.Core.Collectors
{
    public class WindowsCollector
    {
        public (string Version, WindowsLicenseResult License) Collect()
        {
            var version = Utils.SystemHelper.GetOS();

            var checker = new WindowsChecker();
            var license = checker.CheckSystem();

            return (version, license);
        }
    }
}