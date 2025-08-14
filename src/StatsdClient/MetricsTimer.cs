using System;
using System.Collections.Generic;

namespace StatsdClient
{
    internal class MetricsTimer : IDisposable
    {
        private readonly string _name;
        private readonly DogStatsdService _dogStatsd;
        private readonly Stopwatch _stopWatch;
        private readonly double _sampleRate;
        private readonly Cardinality? _cardinality;
        private bool _disposed;

        public MetricsTimer(string name, double sampleRate = 1.0, string[] tags = null, Cardinality? cardinality = null)
            : this(null, name, sampleRate, tags, cardinality)
        {
        }

        public MetricsTimer(DogStatsdService dogStatsd, string name, double sampleRate = 1.0, string[] tags = null, Cardinality? cardinality = null)
        {
            _name = name;
            _dogStatsd = dogStatsd;
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            _sampleRate = sampleRate;
            _cardinality = cardinality;
            Tags = new List<string>();
            if (tags != null)
            {
                Tags.AddRange(tags);
            }
        }

        public List<string> Tags { get; set; }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _stopWatch.Stop();

                if (_dogStatsd == null)
                {
                    DogStatsd.Timer(_name, _stopWatch.ElapsedMilliseconds(), _sampleRate, Tags.ToArray(), _cardinality);
                }
                else
                {
                    _dogStatsd.Timer(_name, _stopWatch.ElapsedMilliseconds(), _sampleRate, Tags.ToArray(), _cardinality);
                }
            }
        }
    }
}