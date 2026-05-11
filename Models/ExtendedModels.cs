namespace NetScanAnalyzer.Models
{
    public class GeoInfo
    {
        public string Country { get; set; } = "";
        public string Region { get; set; } = "";
        public string City { get; set; } = "";
        public string Org { get; set; } = "";
        public string AS { get; set; } = "";
        public string Isp { get; set; } = "";
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class SslInfo
    {
        public string Subject { get; set; } = "";
        public string Issuer { get; set; } = "";
        public DateTime NotBefore { get; set; }
        public DateTime NotAfter { get; set; }
        public bool IsExpired => DateTime.UtcNow > NotAfter;
        public int DaysUntilExpiry => (NotAfter - DateTime.UtcNow).Days;
        public string Protocol { get; set; } = "";
        public string CipherSuite { get; set; } = "";
        public bool IsWeakCipher { get; set; }
        public string[] SAN { get; set; } = Array.Empty<string>();
    }

    public class CveEntry
    {
        public string CveId { get; set; } = "";
        public string Description { get; set; } = "";
        public double CvssScore { get; set; }
        public string Severity => CvssScore switch
        {
            >= 9.0 => "CRITICAL",
            >= 7.0 => "HIGH",
            >= 4.0 => "MEDIUM",
            >= 0.1 => "LOW",
            _ => "NONE"
        };
    }

    public class ThreatAlert
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Type { get; set; } = "";
        public string Source { get; set; } = "";
        public string Destination { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH, CRITICAL
    }

    public class ReconstructedSession
    {
        public string Protocol { get; set; } = "";
        public string ClientIp { get; set; } = "";
        public string ServerIp { get; set; } = "";
        public int ServerPort { get; set; }
        public string Content { get; set; } = "";
        public DateTime Start { get; set; }
        public List<string> CredentialsFound { get; set; } = new();
        public string Ja3Hash { get; set; } = "";
    }

    public class ScanSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string IpRange { get; set; } = "";
        public int HostsFound { get; set; }
        public int TotalOpenPorts { get; set; }
        public List<NetworkHost> Hosts { get; set; } = new();
        public string Notes { get; set; } = "";
    }
}
