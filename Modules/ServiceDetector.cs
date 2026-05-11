using System.Net.Sockets;
using System.Text;
using NetScanAnalyzer.Models;

namespace NetScanAnalyzer.Modules
{
    public class ServiceDetector
    {
        public async Task<string> GrabBannerAsync(string ip, int port, int timeout = 2000)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ip, port);
                if (await Task.WhenAny(connectTask, Task.Delay(timeout)) != connectTask)
                    return string.Empty;

                using var stream = client.GetStream();
                stream.ReadTimeout = timeout;

                // For some protocols we need to send something first (e.g. HTTP)
                if (port == 80 || port == 443 || port == 8080)
                {
                    byte[] request = Encoding.ASCII.GetBytes("HEAD / HTTP/1.1\r\nHost: " + ip + "\r\n\r\n");
                    await stream.WriteAsync(request, 0, request.Length);
                }

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        public string FingerprintBanner(string banner)
        {
            if (string.IsNullOrEmpty(banner)) return "Unknown";

            if (banner.Contains("SSH")) return "SSH Server";
            if (banner.Contains("HTTP")) return "Web Server";
            if (banner.Contains("FTP")) return "FTP Server";
            if (banner.Contains("MySQL")) return "MySQL Database";
            if (banner.Contains("PostgreSQL")) return "PostgreSQL Database";
            
            return "Generic Service";
        }
    }
}
