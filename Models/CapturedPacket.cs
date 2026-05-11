namespace NetScanAnalyzer.Models
{
    public class CapturedPacket
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public int Length { get; set; }
        public string Info { get; set; } = string.Empty;
        public byte[] RawData { get; set; } = Array.Empty<byte>();

        public string HexView => BitConverter.ToString(RawData).Replace("-", " ");
    }
}
