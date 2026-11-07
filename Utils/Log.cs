using System;
using System.IO;

namespace SoftAudit.Utils
{
    /// <summary>
    /// Console + file log, same format as SM-Tool's Write-Log:
    ///   2026-11-07 16:55:01 [INFO   ] [HOT v2.0.0] message
    /// File: C:\SoftAudit\HOST_yyyyMMdd.log (next to the Excel files), else %TEMP%\SoftAudit; if neither is writable, console only.
    /// Logging must never break the audit, so every file error is swallowed.
    /// </summary>
    public static class Log
    {
        public static string Site { get; set; } = "";
        public static string? FilePath { get; private set; }
        static bool _resolved;

        /// <summary>Console line as-is; file line with timestamp/level/site.</summary>
        public static void Info(string message, bool console = true) => Write("INFO", message, console);
        public static void Warn(string message, bool console = true) => Write("WARNING", message, console);
        public static void Error(string message, bool console = true) => Write("ERROR", message, console);

        static void Write(string level, string message, bool console)
        {
            if (console)
                Console.WriteLine(message);

            try
            {
                var path = Resolve();
                if (path == null)
                    return;

                var site = Site == "" ? "" : Site + " ";
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level,-7}] [{site}v{Program.ToolVersion}] " +
                           message.Replace("\r", " ").Replace("\n", " ").Trim();
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch
            {
            }
        }

        static string? Resolve()
        {
            if (_resolved)
                return FilePath;
            _resolved = true;

            var name = $"{Environment.MachineName}_{DateTime.Now:yyyyMMdd}.log";
            foreach (var dir in new[] { @"C:\SoftAudit", Path.Combine(Path.GetTempPath(), "SoftAudit") })
            {
                try
                {
                    Directory.CreateDirectory(dir);
                    var path = Path.Combine(dir, name);
                    File.AppendAllText(path, "");   // proves the folder is writable
                    return FilePath = path;
                }
                catch
                {
                }
            }
            return null;
        }
    }
}
