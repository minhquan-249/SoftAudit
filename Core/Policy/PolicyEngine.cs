using SoftAudit.Core.Models;
using SoftAudit.Config;

namespace SoftAudit.Core.Policy
{
    public class PolicyEngine
    {
        private readonly SoftwareRules _rules;

        public PolicyEngine(SoftwareRules rules)
        {
            _rules = rules;
        }

        public bool ShouldKeep(Software s)
        {
            string name = s.Name.ToLower();

            if (string.IsNullOrEmpty(name))
                return false;

            // allow list (priority cao nhất)
            foreach (var include in _rules.InclusionRules)
            {
                if (name.Contains(include))
                    return true;
            }

            // block list
            foreach (var exclude in _rules.ExclusionRules)
            {
                if (name.Contains(exclude))
                    return false;
            }

            return true;
        }
    }
}