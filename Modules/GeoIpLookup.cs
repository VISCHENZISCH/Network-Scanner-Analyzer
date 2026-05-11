using System.Net.Http;
using System.Text.Json;
using NetScanAnalyzer.Models;

namespace NetScanAnalyzer.Modules
{
    public class GeoIpLookup
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

        public async Task<GeoInfo?> LookupAsync(string ip)
        {
            try
            {
                // Skip private ranges
                if (IsPrivate(ip)) return new GeoInfo { Country = "Local Network", Org = "Private" };

                var json = await _http.GetStringAsync(
                    $"http://ip-api.com/json/{ip}?fields=country,regionName,city,org,as,isp,lat,lon,status");

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var status) && status.GetString() != "success")
                    return null;

                return new GeoInfo
                {
                    Country   = root.TryGetProperty("country",    out var v1) ? v1.GetString() ?? "" : "",
                    Region    = root.TryGetProperty("regionName", out var v2) ? v2.GetString() ?? "" : "",
                    City      = root.TryGetProperty("city",       out var v3) ? v3.GetString() ?? "" : "",
                    Org       = root.TryGetProperty("org",        out var v4) ? v4.GetString() ?? "" : "",
                    AS        = root.TryGetProperty("as",         out var v5) ? v5.GetString() ?? "" : "",
                    Isp       = root.TryGetProperty("isp",        out var v6) ? v6.GetString() ?? "" : "",
                    Lat       = root.TryGetProperty("lat",        out var v7) ? v7.GetDouble() : 0,
                    Lon       = root.TryGetProperty("lon",        out var v8) ? v8.GetDouble() : 0,
                };
            }
            catch { return null; }
        }

        public async Task<string> WhoisAsync(string ip)
        {
            try
            {
                using var tcp = new System.Net.Sockets.TcpClient();
                await tcp.ConnectAsync("whois.iana.org", 43);
                using var stream = tcp.GetStream();
                var query = System.Text.Encoding.ASCII.GetBytes(ip + "\r\n");
                await stream.WriteAsync(query);
                using var reader = new System.IO.StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch { return "WHOIS unavailable"; }
        }

        private bool IsPrivate(string ip)
        {
            if (!System.Net.IPAddress.TryParse(ip, out var addr)) return false;
            byte[] b = addr.GetAddressBytes();
            return (b[0] == 10) ||
                   (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
                   (b[0] == 192 && b[1] == 168) ||
                   (b[0] == 127);
        }
    }
}
