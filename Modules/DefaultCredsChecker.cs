using Renci.SshNet;
using System.Net.Sockets;
using System.Text;

namespace NetScanAnalyzer.Modules
{
    public class DefaultCredsChecker
    {
        private static readonly (string User, string Pass)[] CommonCreds = {
            ("admin", "admin"), ("admin", "password"), ("admin", ""),
            ("root", "root"), ("root", ""), ("root", "toor"),
            ("admin", "1234"), ("admin", "admin123"), ("user", "user"),
            ("administrator", "administrator"), ("guest", "guest"),
            ("pi", "raspberry"), ("cisco", "cisco"), ("ubnt", "ubnt")
        };

        public async Task<List<string>> CheckSshAsync(string ip, int port = 22)
        {
            var found = new List<string>();
            foreach (var (user, pass) in CommonCreds)
            {
                try
                {
                    using var client = new SshClient(ip, port, user, pass);
                    client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(3);
                    client.Connect();
                    if (client.IsConnected)
                    {
                        found.Add($"SSH {user}:{pass}");
                        client.Disconnect();
                    }
                }
                catch { /* wrong creds or unreachable */ }
            }
            return found;
        }

        public async Task<List<string>> CheckHttpAsync(string ip, int port = 80)
        {
            var found = new List<string>();
            var scheme = port == 443 ? "https" : "http";
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

            foreach (var (user, pass) in CommonCreds)
            {
                try
                {
                    var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pass}"));
                    http.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", creds);
                    var resp = await http.GetAsync($"{scheme}://{ip}:{port}/");
                    if (resp.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                        found.Add($"HTTP {user}:{pass} → {resp.StatusCode}");
                }
                catch { }
            }
            return found;
        }

        public async Task<List<string>> CheckAllAsync(string ip, IEnumerable<int> openPorts)
        {
            var all = new List<string>();
            var ports = openPorts.ToList();

            if (ports.Contains(22)) all.AddRange(await CheckSshAsync(ip));
            if (ports.Contains(80)) all.AddRange(await CheckHttpAsync(ip, 80));
            if (ports.Contains(443)) all.AddRange(await CheckHttpAsync(ip, 443));
            if (ports.Contains(8080)) all.AddRange(await CheckHttpAsync(ip, 8080));

            return all;
        }
    }
}
