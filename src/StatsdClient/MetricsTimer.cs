using System.Collections.Concurrent;

namespace StatsdClient
{
    public class MetricsTimer : IMetricsTimer
    {
        private readonly string _name;
        private readonly Stopwatch _stopWatch;
        private bool _disposed;
        private readonly double _sampleRate;
        private readonly ConcurrentBag<string> _tags;

        public MetricsTimer(string name, double sampleRate = 1.0, string[] tags = null)
        {
            _name = name;
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            _sampleRate = sampleRate;
            _tags = tags == null ? new ConcurrentBag<string>() : new ConcurrentBag<string>(tags);
        }

        public void AddTag(params string[] additionalTags)
        {
            if (_disposed || additionalTags == null)
                return;

            foreach (var additionalTag in additionalTags)
            {
                _tags.Add(additionalTag);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _stopWatch.Stop();
            DogStatsd.Timer(_name, _stopWatch.ElapsedMilliseconds(), _sampleRate, _tags.Count > 0 ? _tags.ToArray() : null);
        }
    }
}