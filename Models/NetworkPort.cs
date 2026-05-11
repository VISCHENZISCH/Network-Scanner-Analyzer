namespace NetScanAnalyzer.Models
{
    public enum PortStatus
    {
        Open,
        Closed,
        Filtered
    }

    public class NetworkPort
    {
        public int PortNumber { get; set; }
        public string Protocol { get; set; } = "TCP";
        public PortStatus Status { get; set; } = PortStatus.Closed;
        public string ServiceName { get; set; } = "Unknown";
        public string Banner { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public double ResponseTimeMs { get; set; }

        public string DisplayName => $"{PortNumber}/{Protocol}";
    }
}
