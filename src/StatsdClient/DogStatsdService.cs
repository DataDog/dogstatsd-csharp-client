using System;
using StatsdClient.Bufferize;

namespace StatsdClient
{
    public class DogStatsdService : IDogStatsd, IDisposable
    {
        private StatsdBuilder _statsdBuilder = new StatsdBuilder(new StatsBufferizeFactory());
        private Statsd _statsD;
        private StatsdData _statsdData;
        private string _prefix;
        private StatsdConfig _config;


        public void Configure(StatsdConfig config)
        {
            if (_statsdBuilder == null)
            {
                throw new ObjectDisposedException(nameof(DogStatsdService));
            }

            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (_config != null)
            {
                throw new InvalidOperationException("Configuration for DogStatsdService already performed");
            }

            _config = config;
            _prefix = config.Prefix;

            _statsdData = _statsdBuilder.BuildStatsData(config);
            _statsD = _statsdData.Statsd;
        }

        public void Event(string title, string text, string alertType = null, string aggregationKey = null, string sourceType = null, int? dateHappened = null, string priority = null, string hostname = null, string[] tags = null)
        {
            _statsD?.Send(title, text, alertType, aggregationKey, sourceType, dateHappened, priority, hostname, tags);
        }


        public void Counter<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null)
        {
            _statsD?.Send<Statsd.Counting, T>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }

        public void Increment(string statName, int value = 1, double sampleRate = 1.0, string[] tags = null)
        {
            _statsD?.Send<Statsd.Counting, int>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }

        public void Decrement(string statName, int value = 1, double sampleRate = 1.0, string[] tags = null)
        {
            _statsD?.Send<Statsd.Counting, int>(BuildNamespacedStatName(statName), -value, sampleRate, tags);
        }

        public void Gauge<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null)
        {
            _statsD?.Send<Statsd.Gauge, T>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }

        public void Histogram<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null)
        {
            _statsD?.Send<Statsd.Histogram, T>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }

        public void Distribution<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null)
        {
            _statsD?.Send<Statsd.Distribution, T>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }

        public void Set<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null)
        {
            _statsD?.Send<Statsd.Set, T>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }

        public void Timer<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null)
        {
            _statsD?.Send<Statsd.Timing, T>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }

        public IDisposable StartTimer(string name, double sampleRate = 1.0, string[] tags = null)
        {
            return new MetricsTimer(this, name, sampleRate, tags);
        }

        public void Time(Action action, string statName, double sampleRate = 1.0, string[] tags = null)
        {
            if (_statsD == null)
            {
                action();
            }
            else
            {
                _statsD.Send(action, BuildNamespacedStatName(statName), sampleRate, tags);
            }
        }

        public T Time<T>(Func<T> func, string statName, double sampleRate = 1.0, string[] tags = null)
        {
            if (_statsD == null)
            {
                return func();
            }

            using (StartTimer(statName, sampleRate, tags))
            {
                return func();
            }
        }

        public void ServiceCheck(string name, Status status, int? timestamp = null, string hostname = null, string[] tags = null, string message = null)
        {
            _statsD?.Send(name, (int)status, timestamp, hostname, tags, message);
        }

        private string BuildNamespacedStatName(string statName)
        {
            if (string.IsNullOrEmpty(_prefix))
            {
                return statName;
            }

            return _prefix + "." + statName;
        }

        public ITelemetryCounters TelemetryCounters => _statsdData?.Telemetry;

        public void Dispose()
        {
            _statsdData?.Dispose();
            _statsdData = null;
        }
    }
}
