using System.Net.Http;
using System.Text.Json;

namespace NetScanAnalyzer.Modules
{
    public class ShodanClient
    {
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };
        private readonly string _apiKey;

        public ShodanClient(string apiKey) => _apiKey = apiKey;

        public bool HasKey => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<string> LookupAsync(string ip)
        {
            if (!HasKey) return "No Shodan API key configured.";
            try
            {
                var json = await _http.GetStringAsync(
                    $"https://api.shodan.io/shodan/host/{ip}?key={_apiKey}");
                using var doc = JsonDocument.Parse(json);
                var sb = new System.Text.StringBuilder();

                if (doc.RootElement.TryGetProperty("org", out var org))
                    sb.AppendLine($"Org: {org.GetString()}");
                if (doc.RootElement.TryGetProperty("os", out var os) && os.ValueKind != JsonValueKind.Null)
                    sb.AppendLine($"OS: {os.GetString()}");
                if (doc.RootElement.TryGetProperty("ports", out var ports))
                    sb.AppendLine($"Shodan Ports: {string.Join(", ", ports.EnumerateArray().Select(p => p.GetInt32()))}");
                if (doc.RootElement.TryGetProperty("vulns", out var vulns))
                    sb.AppendLine($"Known Vulns: {string.Join(", ", vulns.EnumerateObject().Select(v => v.Name))}");
                if (doc.RootElement.TryGetProperty("tags", out var tags))
                    sb.AppendLine($"Tags: {string.Join(", ", tags.EnumerateArray().Select(t => t.GetString()))}");

                return sb.Length > 0 ? sb.ToString() : "No Shodan data found.";
            }
            catch (Exception ex) { return $"Shodan error: {ex.Message}"; }
        }
    }
}
