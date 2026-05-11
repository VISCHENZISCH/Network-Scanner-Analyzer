using System.Net;
using System.Net.NetworkInformation;
using NetScanAnalyzer.Models;

namespace NetScanAnalyzer.Modules
{
    public class HostDiscovery
    {
        public async Task<NetworkHost?> PingHostAsync(IPAddress ip, int timeout = 1000)
        {
            using var ping = new Ping();
            try
            {
                var reply = await ping.SendPingAsync(ip, timeout);
                if (reply.Status == IPStatus.Success)
                {
                    var host = new NetworkHost
                    {
                        IPAddress = ip.ToString(),
                        IsOnline = true
                    };

                    try
                    {
                        var entry = await Dns.GetHostEntryAsync(ip);
                        host.Hostname = entry.HostName;
                    }
                    catch { /* DNS resolution failed */ }

                    return host;
                }
            }
            catch { /* Ignore ping errors */ }
            return null;
        }

        public async Task<List<NetworkHost>> ScanRangeAsync(List<IPAddress> range, IProgress<int>? progress = null)
        {
            var activeHosts = new List<NetworkHost>();
            int processed = 0;

            var tasks = range.Select(async ip =>
            {
                var host = await PingHostAsync(ip);
                if (host != null)
                {
                    lock (activeHosts) activeHosts.Add(host);
                }
                processed++;
                progress?.Report(processed * 100 / range.Count);
            });

            await Task.WhenAll(tasks);
            return activeHosts.OrderBy(h => h.IPAddress).ToList();
        }
    }
}
