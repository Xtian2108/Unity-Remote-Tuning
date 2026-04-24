using System.Linq;
using System.Net;
using System.Net.Sockets;
namespace RemoteTuning.Core.Utils
{
    /// <summary>
    /// Network utilities.
    /// </summary>
    public static class NetworkUtils
    {
        /// <summary>
        /// Returns the local LAN IP address of the machine.
        /// </summary>
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            // Find first non-loopback IPv4 address
            var ipAddress = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork 
                                   && !IPAddress.IsLoopback(ip));
            return ipAddress?.ToString() ?? "127.0.0.1";
        }
    }
}
