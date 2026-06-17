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

    try
    {
        exporter.Export(result, path);
        Console.WriteLine($"[EXPORT] Success: {path}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[EXPORT] Failed: {ex.Message}");

        string fallback = $@"{folder}\{machine}_{time}.xlsx";

        try
        {
            exporter.Export(result, fallback);
            Console.WriteLine($"[EXPORT] Fallback success: {fallback}");
        }
        catch
        {
            Console.WriteLine("[EXPORT] Fallback export failed.");
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