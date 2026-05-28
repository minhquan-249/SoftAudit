
using System.Diagnostics;
using System.IO;

namespace SoftAudit.License.Checkers
{
    public class OfficeChecker
    {
        public string CheckSystem()
        {
            try
            {
                var path = FindOspp();
                if (string.IsNullOrEmpty(path))
                    return "Unknown";

                var output = Run("cscript", $"\"{path}\" /dstatus");

                var lower = output.ToLower();

                if (lower.Contains("licensed"))
                {
                    if (lower.Contains("kms"))
                        return "Activated (Unverified)";

                    return "Activated";
                }

                return "Not Activated";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string FindOspp()
        {
            string[] paths =
            {
                @"C:\Program Files\Microsoft Office\Office16\ospp.vbs",
                @"C:\Program Files (x86)\Microsoft Office\Office16\ospp.vbs",
                @"C:\Program Files\Microsoft Office\root\Office16\ospp.vbs",
                @"C:\Program Files (x86)\Microsoft Office\root\Office16\ospp.vbs"
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return "";
        }

        private string Run(string file, string args)
        {
            var p = new Process();

            p.StartInfo.FileName = file;
            p.StartInfo.Arguments = args;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            p.Start();

            return p.StandardOutput.ReadToEnd();
        }
    }
}
