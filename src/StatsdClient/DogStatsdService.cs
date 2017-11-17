using System;

namespace StatsdClient
{
    public class DogStatsdService : IDogStatsd, IDisposable
    {
        private IDisposable _disposable;
        private Statsd _statsD;
        private string _prefix;

        public void Configure(StatsdConfig config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            if (string.IsNullOrEmpty(config.StatsdServerName))
                throw new ArgumentNullException("config.StatsdServername");

            _prefix = config.Prefix;
            Dispose();
            if (!string.IsNullOrEmpty(config.StatsdServerName))
            {
                var statsdUdp = new StatsdUDP(config.StatsdServerName, config.StatsdPort, config.StatsdMaxUDPPacketSize);
                _statsD = new Statsd(statsdUdp);
                _statsD.TruncateIfTooLong = config.StatsdTruncateIfTooLong;
                _disposable = statsdUdp;
            }
        }

        public void Event(string title, string text, string alertType = null, string aggregationKey = null, string sourceType = null, int? dateHappened = null, string priority = null, string hostname = null, string[] tags = null)
        {
            if (_statsD == null)
            {
                return;
            }
            _statsD.Send(title, text, alertType, aggregationKey, sourceType, dateHappened, priority, hostname, tags);
        }


        public void Counter<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null)
        {
            if (_statsD == null)
            {
                return;
            }
            _statsD.Send<Statsd.Counting, T>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }

        public void Increment(string statName, int value = 1, double sampleRate = 1.0, string[] tags = null)
        {
            if (_statsD == null)
            {
                return;
            }
            _statsD.Send<Statsd.Counting, int>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }

        public void Decrement(string statName, int value = 1, double sampleRate = 1.0, params string[] tags)
        {
            if (_statsD == null)
            {
                return;
            }
            _statsD.Send<Statsd.Counting, int>(BuildNamespacedStatName(statName), -value, sampleRate, tags);
        }

        public void Gauge<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null)
        {
            if (_statsD == null)
            {
                return;
            }
            _statsD.Send<Statsd.Gauge, T>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }

        public void Histogram<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null)
        {
            if (_statsD == null)
            {
                return;
            }
            _statsD.Send<Statsd.Histogram, T>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }

        public void Set<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null)
        {
            if (_statsD == null)
            {
                return;
            }
            _statsD.Send<Statsd.Set, T>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }

        public void Timer<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null)
        {
            if (_statsD == null)
            {
                return;
            }

            _statsD.Send<Statsd.Timing, T>(BuildNamespacedStatName(statName), value, sampleRate, tags);
        }


        public IDisposable StartTimer(string name, double sampleRate = 1.0, string[] tags = null)
        {
            return new MetricsTimer(name, sampleRate, tags);
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
            if (_statsD == null)
            {
                return;
            }

            _statsD.Send(name, (int)status, timestamp, hostname, tags, message);
        }

        private string BuildNamespacedStatName(string statName)
        {
            if (string.IsNullOrEmpty(_prefix))
            {
                return statName;
            }

            return _prefix + "." + statName;
        }

        public void Dispose()
        {
            var disposable = _disposable;
            _disposable = null;
            _statsD = null;
            disposable?.Dispose();
        }
    }
}
