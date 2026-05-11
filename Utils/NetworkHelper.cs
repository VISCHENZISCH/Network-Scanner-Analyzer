using System.Net;
using System.Net.Sockets;

namespace NetScanAnalyzer.Utils
{
    public static class NetworkHelper
    {
        public static List<IPAddress> ParseIPRange(string range)
        {
            var ips = new List<IPAddress>();

            if (range.Contains("/"))
            {
                // CIDR notation (e.g., 192.168.1.0/24)
                ips.AddRange(GetIPsFromCIDR(range));
            }
            else if (range.Contains("-"))
            {
                // Range notation (e.g., 192.168.1.1-192.168.1.50)
                ips.AddRange(GetIPsFromRange(range));
            }
            else if (IPAddress.TryParse(range, out var singleIp))
            {
                ips.Add(singleIp);
            }

            return ips;
        }

        private static List<IPAddress> GetIPsFromCIDR(string cidr)
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var ipAddress) || !int.TryParse(parts[1], out var netmask))
                return new List<IPAddress>();

            uint ip = BitConverter.ToUInt32(ipAddress.GetAddressBytes().Reverse().ToArray(), 0);
            uint mask = uint.MaxValue << (32 - netmask);
            uint start = ip & mask;
            uint end = ip | ~mask;

            var result = new List<IPAddress>();
            for (uint i = start; i <= end; i++)
            {
                byte[] bytes = BitConverter.GetBytes(i).Reverse().ToArray();
                result.Add(new IPAddress(bytes));
            }
            return result;
        }

        private static List<IPAddress> GetIPsFromRange(string range)
        {
            var parts = range.Split('-');
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0].Trim(), out var startIp) || !IPAddress.TryParse(parts[1].Trim(), out var endIp))
                return new List<IPAddress>();

            uint start = BitConverter.ToUInt32(startIp.GetAddressBytes().Reverse().ToArray(), 0);
            uint end = BitConverter.ToUInt32(endIp.GetAddressBytes().Reverse().ToArray(), 0);

            var result = new List<IPAddress>();
            for (uint i = start; i <= end; i++)
            {
                byte[] bytes = BitConverter.GetBytes(i).Reverse().ToArray();
                result.Add(new IPAddress(bytes));
            }
            return result;
        }
    }
}
