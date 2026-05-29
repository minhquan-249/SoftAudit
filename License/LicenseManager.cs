using SoftAudit.Core.Models;
// using SoftAudit.Config;
using SoftAudit.License.Checkers;
// using System.Collections.Generic;

namespace SoftAudit.License
{
    public class LicenseManager
    {
        private readonly SoftwareRules _rules;

        // private readonly WindowsChecker _windows;
        private readonly OfficeChecker _office;
        // private readonly AutoCADChecker _autocad;

        // private string _windowsStatus = "Unknown";
        private string _officeStatus = "Unknown";
        // private string _autocadStatus = "Unknown";

        public LicenseManager(SoftwareRules rules)
        {
            _rules = rules;

            // _windows = new WindowsChecker();
            _office = new OfficeChecker();
            // _autocad = new AutoCADChecker();
        }

        public void Apply(List<Software> list)
        {
            // run once
            // _windowsStatus = _windows.CheckSystem();
            _officeStatus = _office.CheckSystem();
            // _autocadStatus = _autocad.CheckSystem();

            foreach (var s in list)
            {
                var type = DetectLicenseType(s);

                // if (type == "Windows")
                // {
                //     s.LicenseStatus = _windowsStatus;
                // }
                // else
                if (type == "Office")
                {
                    s.LicenseStatus = _officeStatus;
                }
                // else if (type == "AutoCAD")
                // {
                //     s.LicenseStatus = _autocadStatus;
                // }
            }
        }

        private string DetectLicenseType(Software s)
        {
            string name = s.Name.ToLower();

            foreach (var rule in _rules.LicenseRules)
            {
                if (name.Contains(rule.Key))
                {
                    return rule.Value;
                }
            }

            return "";
        }
    }
}