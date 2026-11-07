using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SoftAudit.Utils
{
    /// <summary>
    /// Two ways to find the machine's main IPv4:
    /// - Client: the routed source address (respects route + interface metric, so LAN beats Wi-Fi when
    ///   both are up), but only if it belongs to an Up physical adapter with a gateway; otherwise the first
    ///   such adapter (v1.2/v1.4 method), so a FortiClient VPN or VMware adapter is never reported.
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
            string? ip, how;
            if (isServer)
            {
                ip = FromRoute(); how = "route";
                if (ip == null) { ip = AdapterIPs().FirstOrDefault(); how = "adapters"; }
            }
            else
            {
                var route = FromRoute();
                var adapters = AdapterIPs();
                Log.Info($"Client IP candidates: route {route ?? "none"}; adapters {(adapters.Length == 0 ? "none" : string.Join(", ", adapters))}", console: false);
                if (route != null && adapters.Contains(route)) { ip = route; how = "route"; }
                else if (adapters.Length > 0) { ip = adapters[0]; how = "adapters"; }
                else { ip = route; how = "route"; }
            }
            if (ip == null) { ip = FromDns(); how = "dns"; }

            Log.Info($"IP {(ip ?? "NoIP")} via {(ip == null ? "none" : how)} ({(isServer ? "server" : "client")} order)", console: false);
            return ip ?? "NoIP";
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

        /// <summary>IPv4 of every Up, non-virtual/VPN adapter that has a gateway, in Windows' listing order.</summary>
        static string[] AdapterIPs()
        {
            var list = new System.Collections.Generic.List<string>();
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
                        list.Add(ip.ToString());
                }
            }
            catch
            {
            }
            return list.ToArray();
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
