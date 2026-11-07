using SoftAudit.Core;
using SoftAudit.Exporter;
using SoftAudit.Utils;
using System;

Console.WriteLine("SoftAudit v1.4.0");
Console.WriteLine("Starting audit...\n");

try
{

    // -------------------------
    // RUN AUDIT
    // -------------------------
    var service = new AuditService();
    var result = service.Run();

    // -------------------------
    // BUILD FILE PATH
    // -------------------------
    var machine = result.HostName;

    string ip = result.IPv4.Replace(":", "_");

    var time = DateTime.Now.ToString("yyyyMMdd");

    string folder = @"C:\SoftAudit";

    if (!Directory.Exists(folder))
    {
        Directory.CreateDirectory(folder);
    }

    string path = $@"{folder}\{machine}_{ip}_{time}.xlsx";

    var exporter = new ExcelExporter();

    string networkPath = @"\\10.168.109.254\Software\1. Tool Soft Audit\Data";

    string fileName = $"{machine}_{ip}_{time}.xlsx";

    string networkFile = Path.Combine(networkPath, fileName);
    string localFile = Path.Combine(folder, fileName);

    try
    {
        // -------------------------
        // TRY NETWORK FIRST
        // -------------------------

        if (Directory.Exists(networkPath))
        {
            exporter.Export(result, networkFile);
            Console.WriteLine($"[EXPORT] Network success: {networkFile}");
        }
        else
        {
            throw new Exception("Network path not reachable");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[NETWORK] Failed: {ex.Message}");

        // -------------------------
        // FALLBACK LOCAL
        // -------------------------
        try
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            exporter.Export(result, localFile);
            Console.WriteLine($"[EXPORT] Local fallback: {localFile}");
        }
        catch (Exception inner)
        {
            Console.WriteLine($"[EXPORT] Local fallback failed: {inner.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("[FATAL] Unexpected error:");
    Console.WriteLine(ex.Message);
}
finally
{
    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
}