using SoftAudit.Core.Pipeline;
using SoftAudit.Exporter;
using SoftAudit.Utils;

Console.WriteLine("SoftAudit v1.2.0");
Console.WriteLine("SoftAudit started...\n");

try
{
    var pipeline = new SoftwarePipeline();
    var softwares = pipeline.Execute();

    var machine = Environment.MachineName;

    string ip;
    try
    {
        ip = NetworkHelper.GetActiveIP();
        ip = ip.Replace(":", "_");
    }
    catch
    {
        ip = "NoIP";
    }

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
        exporter.Export(softwares, path);
        Console.WriteLine($"Exported: {path}");
    }
    catch (Exception  ex)
    {
        Console.WriteLine("\nExport failed: " + ex.Message);

        string fallback = $@"{folder}\{machine}_{time}.xlsx";

        try
        {
            exporter.Export(softwares, fallback);
            Console.WriteLine($"Saved locally: {fallback}");
        }
        catch
        {
            Console.WriteLine("Fallback export also failed.");
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