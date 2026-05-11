using System.Net.Http;
using System.Text.Json;
using NetScanAnalyzer.Models;

namespace NetScanAnalyzer.Modules
{
    public class CveScanner
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
        private static readonly Dictionary<int, string> PortServiceMap = new()
        {
            { 21, "FTP" }, { 22, "OpenSSH" }, { 23, "Telnet" }, { 25, "SMTP" },
            { 53, "BIND" }, { 80, "Apache HTTP" }, { 110, "POP3" }, { 143, "IMAP" },
            { 443, "OpenSSL" }, { 445, "SMB" }, { 3306, "MySQL" }, { 3389, "RDP" },
            { 5432, "PostgreSQL" }, { 8080, "Jetty" }
        };

        public async Task<List<CveEntry>> LookupByPortAsync(int port)
        {
            var service = PortServiceMap.TryGetValue(port, out var svc) ? svc : null;
            if (service == null) return new();
            return await LookupByServiceAsync(service);
        }

        public async Task<List<CveEntry>> LookupByServiceAsync(string serviceName)
        {
            try
            {
                var url = $"https://services.nvd.nist.gov/rest/json/cves/2.0?keywordSearch={Uri.EscapeDataString(serviceName)}&resultsPerPage=5";
                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);

                var results = new List<CveEntry>();
                if (!doc.RootElement.TryGetProperty("vulnerabilities", out var vulns)) return results;

                foreach (var item in vulns.EnumerateArray())
                {
                    if (!item.TryGetProperty("cve", out var cve)) continue;
                    var id = cve.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                    var desc = "";
                    if (cve.TryGetProperty("descriptions", out var descs))
                        desc = descs.EnumerateArray()
                            .FirstOrDefault(d => d.TryGetProperty("lang", out var l) && l.GetString() == "en")
                            .TryGetProperty("value", out var val) ? val.GetString() ?? "" : "";

                    double score = 0;
                    if (cve.TryGetProperty("metrics", out var metrics))
                    {
                        if (metrics.TryGetProperty("cvssMetricV31", out var v31) && v31.GetArrayLength() > 0)
                        {
                            if (v31[0].TryGetProperty("cvssData", out var cd31) && cd31.TryGetProperty("baseScore", out var bs31))
                                score = bs31.GetDouble();
                        }
                        else if (metrics.TryGetProperty("cvssMetricV2", out var v2) && v2.GetArrayLength() > 0)
                        {
                            if (v2[0].TryGetProperty("cvssData", out var cd2) && cd2.TryGetProperty("baseScore", out var bs2))
                                score = bs2.GetDouble();
                        }
                    }

                    results.Add(new CveEntry { CveId = id, Description = desc, CvssScore = score });
                }

                return results;
            }
            catch { return new(); }
        }
    }
}
