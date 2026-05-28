using SoftAudit.Core.Models;
using SoftAudit.Config;
using System.Collections.Generic;

namespace SoftAudit.Core
{
    public class Normalizer
    {
        private readonly SoftwareRules _rules;

        public Normalizer(SoftwareRules rules)
        {
            _rules = rules;
        }

        public List<Software> Normalize(List<Software> list)
        {
            foreach (var s in list)
            {
                s.Name = NormalizeName(s);
            }

            return list;
        }

        private string NormalizeName(Software s)
        {
            var lower = s.RawName.ToLower();

            foreach (var rule in _rules.NameMapping)
            {
                if (lower.Contains(rule.Key))
                {
                    return rule.Value;
                }
            }

            return CleanName(s.RawName);
        }

        private string CleanName(string name)
        {
            return name
                .Replace("(x64)", "")
                .Replace("(x86)", "")
                .Replace("(64-bit)", "")
                .Replace("(32-bit)", "")
                .Replace("(User)", "")
                .Replace("x64", "")
                .Replace("preview", "")
                .Replace("phiên bản", "")
                .Trim();
        }
    }
}