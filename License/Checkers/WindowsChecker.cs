using System.Diagnostics;

namespace SoftAudit.License.Checkers
{
    public class WindowsChecker
    {
        public string CheckSystem()
        {
            try
            {
                var output = Run("cscript", @"//NoLogo C:\Windows\System32\slmgr.vbs /xpr");

                if (output.ToLower().Contains("permanently"))
                    return "Activated";

                return "Not Activated";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string Run(string file, string args)
        {
            var p = new Process();
            p.StartInfo.FileName = file;
            p.StartInfo.Arguments = args;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();

            return p.StandardOutput.ReadToEnd();
        }
    }
}
