using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace SoftAudit.Utils
{
    /// <summary>
    /// Uploads a file to the shared Apps Script web app used by SM-Tool (AppsScript\Code.gs):
    /// form POST with site, fileName, fileData (base64) and subFolder, answered with {"success":true,...}.
    /// Same behaviour as SM-Tool's Send-FileToDrive: TLS 1.2, 120 s timeout, 3 attempts
    /// (safe to retry: the script overwrites a file with the same name).
    /// </summary>
    public static class DriveUploader
    {
        public const string SubFolder = "SoftAudit";

        public static bool Upload(string url, string site, string filePath, int timeoutSec = 120, int maxAttempts = 3)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var body = Encoding.ASCII.GetBytes(BuildForm(site, Path.GetFileName(filePath), File.ReadAllBytes(filePath)));

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "POST";
                    req.ContentType = "application/x-www-form-urlencoded";
                    req.Timeout = req.ReadWriteTimeout = timeoutSec * 1000;
                    req.AllowAutoRedirect = true; // Apps Script answers POST with a 302 to the result page

                    using (var s = req.GetRequestStream())
                        s.Write(body, 0, body.Length);

                    using var resp = (HttpWebResponse)req.GetResponse();
                    using var reader = new StreamReader(resp.GetResponseStream()!);
                    var text = reader.ReadToEnd();

                    if (IsSuccess(text))
                    {
                        Log.Info($"Drive upload OK: {Path.GetFileName(filePath)} ({body.Length} bytes form), attempt {attempt}, {sw.Elapsed.TotalSeconds:0}s, response {Short(text)}", console: false);
                        return true;
                    }

                    Log.Warn($"           Drive refused (attempt {attempt}/{maxAttempts}, {sw.Elapsed.TotalSeconds:0}s): {Short(text)}");
                }
                catch (Exception ex)
                {
                    Log.Warn($"           Drive error (attempt {attempt}/{maxAttempts}, {sw.Elapsed.TotalSeconds:0}s, timeout {timeoutSec}s): {ex.Message}");
                }

                if (attempt < maxAttempts)
                    Thread.Sleep(5000 * attempt);
            }
            return false;
        }

        public static string BuildForm(string site, string fileName, byte[] data) =>
            "site=" + Uri.EscapeDataString(site) +
            "&subFolder=" + Uri.EscapeDataString(SubFolder) +
            "&fileName=" + Uri.EscapeDataString(fileName) +
            "&fileData=" + Uri.EscapeDataString(Convert.ToBase64String(data));

        /// <summary>Response is JSON like {"success":true,"fileId":"..."}; tolerate spacing.</summary>
        public static bool IsSuccess(string json) =>
            json.Replace(" ", "").IndexOf("\"success\":true", StringComparison.OrdinalIgnoreCase) >= 0;

        static string Short(string s) => s.Length <= 200 ? s : s.Substring(0, 200);
    }
}
