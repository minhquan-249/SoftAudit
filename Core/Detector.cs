using SoftAudit.Core.Models;
using SoftAudit.Config;

namespace SoftAudit.Core
{
    public class Detector
    {
        private readonly SoftwareRules _rules;

        public Detector(SoftwareRules rules)
        {
            _rules = rules;
        }
    }
}