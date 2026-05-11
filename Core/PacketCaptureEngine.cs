using SharpPcap;
using PacketDotNet;
using NetScanAnalyzer.Models;

namespace NetScanAnalyzer.Core
{
    public class PacketCaptureEngine
    {
        private ILiveDevice? _device;
        public event Action<CapturedPacket>? OnPacketCaptured;

        public static List<ILiveDevice> GetDevices()
        {
            return CaptureDeviceList.Instance.ToList();
        }

        public void StartCapture(ILiveDevice device)
        {
            _device = device;
            _device.OnPacketArrival += Device_OnPacketArrival;
            _device.Open(DeviceModes.Promiscuous, 1000);
            _device.StartCapture();
        }

        public void StopCapture()
        {
            if (_device != null && _device.Started)
            {
                _device.StopCapture();
                _device.Close();
            }
        }

        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            var rawPacket = e.GetPacket();
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

            var captured = new CapturedPacket
            {
                Timestamp = DateTime.Now,
                Length = rawPacket.Data.Length,
                RawData = rawPacket.Data
            };

            if (packet is EthernetPacket eth)
            {
                captured.Source = eth.SourceHardwareAddress.ToString();
                captured.Destination = eth.DestinationHardwareAddress.ToString();

                var ipPacket = eth.Extract<IPPacket>();
                if (ipPacket != null)
                {
                    captured.Source = ipPacket.SourceAddress.ToString();
                    captured.Destination = ipPacket.DestinationAddress.ToString();
                    captured.Protocol = ipPacket.Protocol.ToString();
                    
                    var tcpPacket = ipPacket.Extract<TcpPacket>();
                    if (tcpPacket != null)
                    {
                        captured.Info = $"TCP {tcpPacket.SourcePort} -> {tcpPacket.DestinationPort}";
                    }
                    
                    var udpPacket = ipPacket.Extract<UdpPacket>();
                    if (udpPacket != null)
                    {
                        captured.Info = $"UDP {udpPacket.SourcePort} -> {udpPacket.DestinationPort}";
                    }
                }
            }

            OnPacketCaptured?.Invoke(captured);
        }
    }
}
