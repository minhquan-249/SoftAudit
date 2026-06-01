using System;
using System.Net;
using System.Net.Sockets;

namespace SoftAudit.Utils
{
    public static class NetworkHelper
    {
    public static string GetActiveIP()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);

            socket.Connect("8.8.8.8", 65530);

            if (socket.LocalEndPoint is IPEndPoint endPoint)
            {
                return endPoint.Address.ToString();
            }
        }
        catch (SocketException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] GetActiveIP failed: {ex.Message}");
        }

        return GetFallbackIP();
    }

        private static string GetFallbackIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());

                foreach (var addr in host.AddressList)
                {
                    if (addr.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    var ip = addr.ToString();

                    if (ip.StartsWith("127.") || ip.StartsWith("169.254"))
                        continue;

                    return ip;
                }
            }
            catch
            {
            }

            return "NoIP";
        }
    }
}
