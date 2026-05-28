using System.ServiceProcess;

namespace SoftAudit.License.Checkers
{
    public class AutoCADChecker
    {
        public string CheckSystem()
        {
            try
            {
                var services = ServiceController.GetServices();

                foreach (var s in services)
                {
                    if (s.ServiceName.ToLower().Contains("autodesk"))
                        return "Activated";
                }

                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
