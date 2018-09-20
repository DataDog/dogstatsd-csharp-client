using System;

namespace StatsdClient
{
    internal class MetricsTimerInternal : IDisposable
    {
        private readonly DogStatsdService _dogStatsdService;
        private readonly string _name;
        private readonly Stopwatch _stopWatch;
        private bool _disposed;
        private readonly double _sampleRate;
        private readonly string[] _tags;

        public MetricsTimerInternal(DogStatsdService dogStatsdService, string name, double sampleRate = 1.0,
            string[] tags = null)
        {
            if (dogStatsdService == null) throw new ArgumentNullException(nameof(dogStatsdService));
            _dogStatsdService = dogStatsdService;
            _name = name;
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            _sampleRate = sampleRate;
            _tags = tags;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _stopWatch.Stop();
                _dogStatsdService.Timer(_name, _stopWatch.ElapsedMilliseconds(), _sampleRate, _tags);
            }
        }
    }
}