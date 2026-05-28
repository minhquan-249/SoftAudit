using SoftAudit.Config;
using SoftAudit.Core.Models;
using SoftAudit.Core.Policy;
using System.Collections.Generic;
using System.Linq;
using SoftAudit.License;

namespace SoftAudit.Core.Pipeline
{
    public class SoftwarePipeline
    {
        private readonly Scanner _scanner;
        private readonly Normalizer _normalizer;
        private readonly PolicyEngine _policy;
        private readonly LicenseManager _license;


        public SoftwarePipeline()
        {
            var basePath = AppContext.BaseDirectory;
            var path = Path.Combine(basePath, "Config", "software_rules.json");
            var rules = RuleLoader.Load(path);

            _scanner = new Scanner();
            _normalizer = new Normalizer(rules);
            _policy = new PolicyEngine(rules);
            _license = new LicenseManager(rules);
        }

        public List<Software> Execute()
        {
            var data = _scanner.ScanInstalledSoftware();

            data = _normalizer.Normalize(data);

            //APPLY POLICY HERE
            data = data
                .Where(s => _policy.ShouldKeep(s))
                .ToList();

            // deduplicate AFTER filter
            data = data
                .GroupBy(x => x.Name.ToLower())
                .Select(x => x.First())
                .ToList();
            _license.Apply(data);
            return data;
        }
    }
}