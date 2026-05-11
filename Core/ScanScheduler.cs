namespace NetScanAnalyzer.Core
{
    public class ScanScheduler : IDisposable
    {
        private System.Threading.Timer? _timer;
        public event Action? OnTrigger;
        public bool IsRunning { get; private set; }
        public TimeSpan Interval { get; private set; }
        public DateTime? NextRun { get; private set; }

        public void Start(TimeSpan interval)
        {
            Interval = interval;
            IsRunning = true;
            NextRun = DateTime.Now + interval;
            _timer = new System.Threading.Timer(_ =>
            {
                NextRun = DateTime.Now + Interval;
                OnTrigger?.Invoke();
            }, null, interval, interval);
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
            IsRunning = false;
            NextRun = null;
        }

        public void Dispose() => Stop();
    }
}
