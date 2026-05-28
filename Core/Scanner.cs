using Microsoft.Win32;
using SoftAudit.Core.Models;
using SoftAudit.Utils;
using System.Collections.Generic;

namespace SoftAudit.Core
{
    public class Scanner
    {
        public List<Software> ScanInstalledSoftware()
        {
            var result = new List<Software>();

            string os = SystemHelper.GetOS();
            string host = SystemHelper.GetHostName();

            string ip;
            try
            {
                ip = NetworkHelper.GetActiveIP();
            }
            catch
            {
                ip = "NoIP";
            }

            string[] registryPaths =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var path in registryPaths)
            {
                ReadFromRegistry(Registry.LocalMachine, path, result, os, host, ip);
            }

            foreach (var path in registryPaths)
            {
                ReadFromRegistry(Registry.CurrentUser, path, result, os, host, ip);
            }

            return result;
        }

        private void ReadFromRegistry(
            RegistryKey root,
            string path,
            List<Software> list,
            string os,
            string host,
            string ip)
        {
            try
            {
                using var key = root.OpenSubKey(path);
                if (key == null)
                    return;

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    var software = ReadSoftwareFromSubKey(key, subKeyName, os, host, ip);
                    if (software != null)
                    {
                        list.Add(software);
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private Software? ReadSoftwareFromSubKey(
            RegistryKey parentKey,
            string subKeyName,
            string os,
            string host,
            string ip)
        {
            try
            {
                using var subKey = parentKey.OpenSubKey(subKeyName);
                if (subKey == null)
                    return null;

                string name = GetString(subKey, "DisplayName");
                if (string.IsNullOrEmpty(name))
                    return null;

                return new Software
                {
                    OS = os,
                    HostName = host,
                    IPv4 = ip,

                    RawName = name,
                    Name = name,

                    Version = GetString(subKey, "DisplayVersion"),
                    Publisher = GetString(subKey, "Publisher"),
                    InstallDate = GetString(subKey, "InstallDate")
                };
            }
            catch
            {
                return null;
            }
        }

        private string GetString(RegistryKey key, string value)
        {
            try
            {
                return key.GetValue(value)?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}