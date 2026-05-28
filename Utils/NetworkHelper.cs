using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SoftAudit.Utils
{
    public static class NetworkHelper
    {
        public static string GetActiveIP()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                var desc = ni.Description.ToLower();

                if (desc.Contains("virtual") ||
                    desc.Contains("vpn") ||
                    desc.Contains("vmware") ||
                    desc.Contains("hyper-v") ||
                    desc.Contains("fortinet") ||
                    desc.Contains("bluetooth"))
                    continue;

                var props = ni.GetIPProperties();

                // must have default gateway (like your PS logic)
                if (props.GatewayAddresses == null || props.GatewayAddresses.Count == 0)
                    continue;

                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    var ip = addr.Address.ToString();

                    if (ip.StartsWith("127.") || ip.StartsWith("169.254"))
                        continue;

                    return ip;
                }
            }

            return "NoIP";
        }
    }
}