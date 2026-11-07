using System;
using System.Linq;
using Microsoft.Win32;
using SoftAudit.Core.Models;
using SoftAudit.License;

namespace SoftAudit.Core.Collectors
{
    public static class OfficeCollector
    {
        static readonly string[] NotOfficeSuite =
            { "update", "click-to-run", "proofing", "language", "mui", "viewer", "compatibility", "web components" };

        public static (string Version, LicenseInfo License) Collect()
        {
            var version = GetOfficeVersion();
            var license = LicenseChecker.CheckOffice();

            // Microsoft 365 Apps signed in with a user account do not register a product key with
            // the Software Protection service, so WMI finds nothing.
            if (license.Activation == "Not Detected" && version.IndexOf("365", StringComparison.Ordinal) >= 0)
                license = new LicenseInfo { Activation = "Microsoft 365 (check portal)", Channel = "Subscription" };

            if (version == "Not Detected")
                license = new LicenseInfo { Activation = "-", Channel = "-" };

            return (version, license);
        }

        /// <summary>
        /// Display name as shown in Programs and Features ("Microsoft Office Professional Plus 2019 - en-us"),
        /// falling back to the Click-to-Run configuration.
        /// </summary>
        static string GetOfficeVersion()
        {
            foreach (var app in SoftwareScanner.ReadUninstallEntries(machineOnly: true))
            {
                var lower = app.RawName.ToLowerInvariant();
                var isSuite = lower.StartsWith("microsoft office") || lower.StartsWith("microsoft 365");
                if (isSuite && !NotOfficeSuite.Any(lower.Contains))
                    return app.RawName.Trim();
            }

            try
            {
                using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");
                var product = key?.GetValue("ProductReleaseIds")?.ToString();
                var build = key?.GetValue("VersionToReport")?.ToString();
                if (!string.IsNullOrWhiteSpace(product))
                    return $"Microsoft Office ({product}) [{build}]";
            }
            catch
            {
            }

            return "Not Detected";
        }
    }
}
