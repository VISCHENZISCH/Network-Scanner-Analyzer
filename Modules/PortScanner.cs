using System.Net;
using System.Net.Sockets;
using NetScanAnalyzer.Models;

namespace NetScanAnalyzer.Modules
{
    public class PortScanner
    {
        public async Task<NetworkPort> ScanPortAsync(string ip, int port, int timeout = 500)
        {
            var result = new NetworkPort { PortNumber = port };
            using var client = new TcpClient();

            try
            {
                var connectTask = client.ConnectAsync(ip, port);
                var delayTask = Task.Delay(timeout);

                if (await Task.WhenAny(connectTask, delayTask) == connectTask)
                {
                    await connectTask; // Ensure any exception is thrown
                    result.Status = PortStatus.Open;
                    result.ServiceName = GuessService(port);
                }
                else
                {
                    result.Status = PortStatus.Filtered;
                }
            }
            catch
            {
                result.Status = PortStatus.Closed;
            }

            return result;
        }

        public async Task<List<NetworkPort>> ScanHostPortsAsync(string ip, IEnumerable<int> ports, IProgress<int>? progress = null)
        {
            var results = new List<NetworkPort>();
            var portList = ports.ToList();
            int processed = 0;

            var tasks = portList.Select(async port =>
            {
                var res = await ScanPortAsync(ip, port);
                lock (results) results.Add(res);
                processed++;
                progress?.Report(processed * 100 / portList.Count);
            });

            await Task.WhenAll(tasks);
            return results.OrderBy(r => r.PortNumber).ToList();
        }

        private string GuessService(int port)
        {
            return port switch
            {
                21 => "FTP",
                22 => "SSH",
                23 => "Telnet",
                25 => "SMTP",
                53 => "DNS",
                80 => "HTTP",
                110 => "POP3",
                143 => "IMAP",
                443 => "HTTPS",
                445 => "SMB",
                3306 => "MySQL",
                3389 => "RDP",
                5432 => "PostgreSQL",
                8080 => "HTTP-Proxy",
                _ => "Unknown"
            };
        }
    }
}
