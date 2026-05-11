namespace NetScanAnalyzer.Models
{
    public class NetworkHost
    {
        public string IPAddress { get; set; } = string.Empty;
        public string MACAddress { get; set; } = "Unknown";
        public string Hostname { get; set; } = "Unknown";
        public string Vendor { get; set; } = "Unknown";
        public string OSFamily { get; set; } = "Unknown";
        public int TTL { get; set; }
        public bool IsOnline { get; set; }
        public List<NetworkPort> OpenPorts { get; set; } = new();
        public DateTime LastScanned { get; set; } = DateTime.Now;

        // Extended info
        public GeoInfo? GeoInfo { get; set; }
        public SslInfo? SslInfo { get; set; }
        public List<CveEntry> Cves { get; set; } = new();
        public int RiskScore { get; set; }
        public string ShodanData { get; set; } = "";

        public string RiskLabel => RiskScore switch
        {
            >= 80 => "CRITICAL",
            >= 60 => "HIGH",
            >= 30 => "MEDIUM",
            _ => "LOW"
        };

        public override string ToString() => $"{IPAddress} ({Hostname})";
    }
}
