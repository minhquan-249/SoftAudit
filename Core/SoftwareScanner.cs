using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using SoftAudit.Core.Models;

namespace SoftAudit.Core
{
    /// <summary>
    /// Installed software from the registry Uninstall keys (same source as Programs and Features).
    /// Ported from v1.3 Scanner + Normalizer + PolicyEngine; the rules that used to live in
    /// Config\software_rules.json are below so the exe needs no config folder.
    /// </summary>
    public static class SoftwareScanner
    {
        // ---- rules (was Config\software_rules.json) ----

        /// <summary>Raw name contains key (lower case) → report as value. First match wins.</summary>
        static readonly (string Contains, string Name)[] NameMapping =
        {
            ("visual studio code", "Visual Studio Code"),
            ("google chrome", "Google Chrome"),
            ("microsoft edge", "Microsoft Edge"),
            ("onedrive", "Microsoft OneDrive"),
            ("teams", "Microsoft Teams"),
            ("winrar", "WinRAR"),
            ("notepad++", "Notepad++"),
            ("java", "Java Runtime"),
        };

        /// <summary>Always kept, even if an exclusion matches.</summary>
        static readonly string[] Inclusions = { "webview2" };

        /// <summary>Dropped when the name contains any of these.</summary>
        static readonly string[] Exclusions =
        {
            "kb", "security update", "hotfix",
            "workload", "manifest",
            "mui", "proofing", "language", "correction",
            "setup metadata", "shared", "component",
            "targeting pack", "apphost", "templates", "toolset", "linguistic",
            "linguistique", "corrección",
        };

        static readonly string[] UninstallPaths =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        };

        /// <summary>Filtered, renamed and de-duplicated list, sorted by name.</summary>
        public static List<Software> Scan()
        {
            var all = ReadUninstallEntries(machineOnly: false).ToList();
            var list = all
                .Where(s => Keep(s.RawName))
                .Select(s => { s.Name = Normalize(s.RawName); return s; })
                .Where(s => s.Name != "")
                .GroupBy(s => s.Name.ToLowerInvariant())
                .Select(g => g.First())
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            Utils.Log.Info($"Software: {all.Count} registry entries -> {list.Count} after filter/dedup", console: false);
            return list;
        }

        /// <summary>Every entry with a DisplayName, from HKLM (64 + 32-bit) and, unless machineOnly, HKCU.</summary>
        public static IEnumerable<Software> ReadUninstallEntries(bool machineOnly)
        {
            var hives = machineOnly
                ? new[] { RegistryHive.LocalMachine }
                : new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser };

            foreach (var hive in hives)
            {
                RegistryKey root;
                try { root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64); }
                catch { continue; }

                using (root)
                {
                    foreach (var path in UninstallPaths)
                    {
                        foreach (var s in ReadKey(root, path))
                            yield return s;
                    }
                }
            }
        }

        static List<Software> ReadKey(RegistryKey root, string path)
        {
            var list = new List<Software>();
            try
            {
                using var key = root.OpenSubKey(path);
                if (key == null)
                    return list;

                foreach (var subName in key.GetSubKeyNames())
                {
                    try
                    {
                        using var sub = key.OpenSubKey(subName);
                        var name = sub?.GetValue("DisplayName")?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(name))
                            continue;

                        list.Add(new Software
                        {
                            RawName = name!,
                            Name = name!,
                            Version = sub!.GetValue("DisplayVersion")?.ToString() ?? "",
                            Publisher = sub.GetValue("Publisher")?.ToString() ?? "",
                            InstallDate = FormatDate(sub.GetValue("InstallDate")?.ToString() ?? ""),
                        });
                    }
                    catch
                    {
                        // one unreadable entry must not stop the scan
                    }
                }
            }
            catch
            {
            }
            return list;
        }

        public static bool Keep(string rawName)
        {
            var name = rawName.ToLowerInvariant();
            if (Inclusions.Any(name.Contains))
                return true;
            return !Exclusions.Any(name.Contains);
        }

        public static string Normalize(string rawName)
        {
            var lower = rawName.ToLowerInvariant();
            foreach (var (contains, mapped) in NameMapping)
            {
                if (lower.Contains(contains))
                    return mapped;
            }

            var name = rawName;
            foreach (var noise in new[] { "(x64)", "(x86)", "(64-bit)", "(32-bit)", "(User)", "x64", "Preview", "preview", "phiên bản" })
                name = name.Replace(noise, "");
            return string.Join(" ", name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>Registry InstallDate is usually yyyyMMdd; show it as yyyy-MM-dd.</summary>
        public static string FormatDate(string raw)
        {
            if (raw.Length == 8 && DateTime.TryParseExact(raw, "yyyyMMdd", null,
                    System.Globalization.DateTimeStyles.None, out var d))
                return d.ToString("yyyy-MM-dd");
            return raw;
        }
    }
}
