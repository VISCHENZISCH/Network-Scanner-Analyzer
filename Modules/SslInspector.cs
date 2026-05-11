using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using NetScanAnalyzer.Models;

namespace NetScanAnalyzer.Modules
{
    public class SslInspector
    {
        private static readonly string[] WeakCiphers = { "RC4", "DES", "3DES", "NULL", "EXPORT", "anon" };

        public async Task<SslInfo?> InspectAsync(string host, int port = 443, int timeout = 3000)
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                var connectTask = client.ConnectAsync(host, port);
                if (await Task.WhenAny(connectTask, Task.Delay(timeout)) != connectTask) return null;

                X509Certificate2? cert = null;
                string protocol = "";
                string cipher = "";

                using var ssl = new SslStream(client.GetStream(), false, (s, c, ch, e) =>
                {
                    cert = c != null ? new X509Certificate2(c) : null;
                    return true;
                });

                await ssl.AuthenticateAsClientAsync(host);
                protocol = ssl.SslProtocol.ToString();
                cipher = ssl.CipherAlgorithm.ToString();

                if (cert == null) return null;

                var san = cert.Extensions
                    .OfType<X509SubjectAlternativeNameExtension>()
                    .SelectMany(e => e.EnumerateDnsNames())
                    .ToArray();

                return new SslInfo
                {
                    Subject = cert.Subject,
                    Issuer = cert.Issuer,
                    NotBefore = cert.NotBefore,
                    NotAfter = cert.NotAfter,
                    Protocol = protocol,
                    CipherSuite = cipher,
                    IsWeakCipher = WeakCiphers.Any(w => cipher.Contains(w, StringComparison.OrdinalIgnoreCase)),
                    SAN = san
                };
            }
            catch { return null; }
        }
    }
}
