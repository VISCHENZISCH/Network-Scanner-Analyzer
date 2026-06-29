using LiteDB;
using NetScanAnalyzer.Models;

namespace NetScanAnalyzer.Core
{
    public class ScanHistory : IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<ScanSession> _sessions;

        public ScanHistory(string? dbPath = null)
        {
            dbPath ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scans.db");
            _db = new LiteDatabase($"Filename={dbPath};Connection=shared");
            _sessions = _db.GetCollection<ScanSession>("sessions");
            _sessions.EnsureIndex(x => x.Timestamp);
        }

        public void Save(ScanSession session) => _sessions.Upsert(session);

        public List<ScanSession> GetAll() =>
            _sessions.FindAll().OrderByDescending(s => s.Timestamp).ToList();

        public ScanSession? GetById(string id) => _sessions.FindById(id);

        public List<ScanSession> GetLast(int n) =>
            _sessions.FindAll().OrderByDescending(s => s.Timestamp).Take(n).ToList();

        public ScanDiff Compare(string sessionId1, string sessionId2)
        {
            var s1 = GetById(sessionId1);
            var s2 = GetById(sessionId2);
            if (s1 == null || s2 == null) return new ScanDiff();

            var ips1 = s1.Hosts.Select(h => h.IPAddress).ToHashSet();
            var ips2 = s2.Hosts.Select(h => h.IPAddress).ToHashSet();

            return new ScanDiff
            {
                NewHosts      = s2.Hosts.Where(h => !ips1.Contains(h.IPAddress)).ToList(),
                RemovedHosts  = s1.Hosts.Where(h => !ips2.Contains(h.IPAddress)).ToList(),
                ChangedHosts  = s2.Hosts.Where(h => ips1.Contains(h.IPAddress) &&
                                    HasPortChanges(s1, h.IPAddress, h.OpenPorts)).ToList()
            };
        }

        private bool HasPortChanges(ScanSession s1, string ip, List<NetworkPort> newPorts)
        {
            var old = s1.Hosts.FirstOrDefault(h => h.IPAddress == ip);
            if (old == null) return false;
            var oldSet = old.OpenPorts.Select(p => $"{p.PortNumber}/{p.Protocol}").ToHashSet();
            var newSet = newPorts.Select(p => $"{p.PortNumber}/{p.Protocol}").ToHashSet();
            return !oldSet.SetEquals(newSet);
        }

        public void Delete(string id) => _sessions.Delete(id);

        public void Dispose() => _db.Dispose();
    }

    public class ScanDiff
    {
        public List<NetworkHost> NewHosts     { get; set; } = new();
        public List<NetworkHost> RemovedHosts { get; set; } = new();
        public List<NetworkHost> ChangedHosts { get; set; } = new();
    }
}
