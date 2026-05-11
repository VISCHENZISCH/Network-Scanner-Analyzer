using System.Net.NetworkInformation;
using NetScanAnalyzer.Models;

namespace NetScanAnalyzer.Modules
{
    public class OsFingerprinter
    {
        public async Task<(string Os, int Ttl)> FingerprintAsync(string ip, int timeout = 1000)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, timeout);
                if (reply.Status != IPStatus.Success) return ("Unknown", 0);

                int ttl = reply.Options?.Ttl ?? 0;
                string os = ttl switch
                {
                    > 0 and <= 64  => "Linux / Android / macOS",
                    > 64 and <= 128 => "Windows",
                    > 128 and <= 255 => "Cisco / Network Device",
                    _ => "Unknown"
                };

                return (os, ttl);
            }
            catch { return ("Unknown", 0); }
        }

        public string FingerprintFromBanner(string banner)
        {
            if (string.IsNullOrEmpty(banner)) return "Unknown";
            banner = banner.ToLower();
            if (banner.Contains("ubuntu") || banner.Contains("debian")) return "Ubuntu/Debian Linux";
            if (banner.Contains("centos") || banner.Contains("rhel") || banner.Contains("fedora")) return "RHEL/CentOS Linux";
            if (banner.Contains("linux")) return "Linux";
            if (banner.Contains("windows") || banner.Contains("microsoft")) return "Windows";
            if (banner.Contains("freebsd") || banner.Contains("openbsd")) return "BSD";
            if (banner.Contains("cisco")) return "Cisco IOS";
            if (banner.Contains("juniper")) return "Juniper";
            return "Unknown";
        }
    }
}
