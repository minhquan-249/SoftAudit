using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SoftAudit.Utils
{
    /// <summary>
    /// Two ways to find the machine's main IPv4:
    /// - Client (v1.2/v1.4 method): first Up adapter with a gateway, skipping virtual/VPN adapters,
    ///   so a FortiClient VPN or VMware adapter is never reported.
    /// - Server (v1.3.1 method): the source address Windows would use to route out. On Hyper-V hosts and
    ///   NIC teams the real IP lives on a "Hyper-V Virtual Ethernet Adapter" / team adapter, which the
    ///   client filter would skip.
    /// Each method falls back to the other, then to DNS.
    /// </summary>
    public static class NetworkHelper
    {
        static readonly string[] SkipAdapters =
            { "virtual", "vpn", "vmware", "hyper-v", "fortinet", "bluetooth", "tap-", "wan miniport" };

        public static string GetActiveIP(bool isServer)
        {
            var ip = isServer
                ? FromRoute() ?? FromAdapters()
                : FromAdapters() ?? FromRoute();

            return ip ?? FromDns() ?? "NoIP";
        }

        /// <summary>MAC (AA-BB-CC-DD-EE-FF) of the adapter that holds <paramref name="ip"/>.</summary>
        public static string GetMac(string ip)
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    var hasIp = ni.GetIPProperties().UnicastAddresses
                        .Any(a => a.Address.ToString() == ip);
                    if (!hasIp)
                        continue;

                    var bytes = ni.GetPhysicalAddress().GetAddressBytes();
                    if (bytes.Length > 0)
                        return string.Join("-", bytes.Select(b => b.ToString("X2")));
                }
            }
            catch
            {
            }
            return "Unknown";
        }

        static string? FromAdapters()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    var desc = (ni.Description + " " + ni.Name).ToLowerInvariant();
                    if (SkipAdapters.Any(desc.Contains))
                        continue;

                    var props = ni.GetIPProperties();
                    if (!props.GatewayAddresses.Any(g => !g.Address.Equals(IPAddress.Any)))
                        continue;

                    var ip = props.UnicastAddresses
                        .Select(a => a.Address)
                        .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && IsUsable(a));
                    if (ip != null)
                        return ip.ToString();
                }
            }
            catch
            {
            }
            return null;
        }

        static string? FromRoute()
        {
            try
            {
                // UDP "connect" sends nothing; it only asks Windows which local address routes to 8.8.8.8.
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint ep && IsUsable(ep.Address))
                    return ep.Address.ToString();
            }
            catch
            {
            }
            return null;
        }

        static string? FromDns()
        {
            try
            {
                return Dns.GetHostAddresses(Dns.GetHostName())
                    .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && IsUsable(a))
                    ?.ToString();
            }
            catch
            {
                return null;
            }
        }

        static bool IsUsable(IPAddress a)
        {
            var s = a.ToString();
            return !s.StartsWith("127.") && !s.StartsWith("169.254.") && s != "0.0.0.0";
        }
    }
}
