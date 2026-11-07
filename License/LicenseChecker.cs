using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using SoftAudit.Core.Models;

namespace SoftAudit.License
{
    /// <summary>
    /// Reads activation from the Software Protection service through WMI instead of parsing
    /// slmgr.vbs / ospp.vbs text, so it works on any display language and any Office version
    /// (2010+). Replaces v1.4's WindowsChecker / OfficeChecker.
    /// </summary>
    public static class LicenseChecker
    {
        const string WindowsAppId = "55c92734-d682-4d71-983e-d6ec3f16059f";
        const string OfficeAppId = "0ff1ce15-a989-479d-af46-f275c6370663";

        public static LicenseInfo CheckWindows() =>
            Best(Query("SoftwareLicensingProduct", WindowsAppId));

        public static LicenseInfo CheckOffice()
        {
            // Office 2013+ on Windows 8+ registers in SoftwareLicensingProduct;
            // Office 2010 and Office on Windows 7 use OfficeSoftwareProtectionProduct.
            var products = Query("SoftwareLicensingProduct", OfficeAppId);
            products.AddRange(Query("OfficeSoftwareProtectionProduct", OfficeAppId));
            return Best(products);
        }

        /// <summary>Licensed product wins; otherwise the first product that has a key installed.</summary>
        static LicenseInfo Best(List<(uint Status, string Description)> products)
        {
            if (products.Count == 0)
                return new LicenseInfo { Activation = "Not Detected", Channel = "-" };

            var p = products.FirstOrDefault(x => x.Status == 1);
            if (p.Description == null)
                p = products[0];

            return new LicenseInfo
            {
                Activation = StatusText(p.Status),
                Channel = ChannelText(p.Description)
            };
        }

        static List<(uint, string)> Query(string wmiClass, string appId)
        {
            var list = new List<(uint, string)>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT Description, LicenseStatus FROM {wmiClass} " +
                    $"WHERE ApplicationID='{appId}' AND PartialProductKey IS NOT NULL");

                foreach (ManagementObject obj in searcher.Get())
                    using (obj)
                        list.Add((Convert.ToUInt32(obj["LicenseStatus"] ?? 0u),
                                  obj["Description"]?.ToString() ?? ""));
            }
            catch
            {
                // class missing (e.g. OfficeSoftwareProtectionProduct on Windows 10) or WMI error
            }
            return list;
        }

        // https://learn.microsoft.com/previous-versions/windows/desktop/sppwmi/softwarelicensingproduct
        public static string StatusText(uint status) => status switch
        {
            0 => "Not Activated",
            1 => "Activated",
            2 => "Grace Period (OOB)",
            3 => "Grace Period (OOT)",
            4 => "Non-Genuine Grace",
            5 => "Not Activated (Notification)",
            6 => "Extended Grace",
            _ => "Unknown"
        };

        /// <summary>
        /// Description looks like "Windows(R) Operating System, VOLUME_KMSCLIENT channel"
        /// or "Office 19, RETAIL channel".
        /// </summary>
        public static string ChannelText(string description)
        {
            var d = description.ToUpperInvariant();
            if (d.Contains("KMS")) return "KMS";
            if (d.Contains("MAK")) return "MAK";
            if (d.Contains("OEM")) return "OEM";
            if (d.Contains("RETAIL")) return "Retail";
            if (d.Contains("TIMEBASED") || d.Contains("SUBSCRIPTION")) return "Subscription";
            return "Unknown";
        }
    }
}
