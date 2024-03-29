using System;
using System.Diagnostics;
using System.Globalization;
using StatsdClient.Bufferize;

namespace StatsdClient
{
    /// <summary>
    /// DogStatsdService is a <a href="https://docs.datadoghq.com/developers/dogstatsd/?tab=net">DogStatsD client</a>.
    /// Dispose must be called to flush all the metrics.
    /// </summary>
    public class DogStatsdService : IDogStatsd, IDisposable
    {
        private StatsdBuilder _statsdBuilder = new StatsdBuilder(new StatsBufferizeFactory());
        private MetricsSender _metricsSender;
        private StatsdData _statsdData;
        private StatsdConfig _config;

        /// <summary>
        /// Gets the telemetry counters
        /// </summary>
        /// <value>The telemetry counters.</value>
        public ITelemetryCounters TelemetryCounters => _statsdData?.Telemetry;

        /// <summary>
        /// Configures the instance.
        /// Must be called before any other methods.
        /// Can only be called once.
        /// </summary>
        /// <param name="config">The value of the config.</param>
        /// <param name="optionalExceptionHandler">The handler called when an error occurs."</param>
        /// <returns>Return true if the operation succeed, false otherwise. If this function fails,
        /// other methods in this class do nothing and an error is reported to <paramref name="optionalExceptionHandler"/>.</returns>
        public bool Configure(StatsdConfig config, Action<Exception> optionalExceptionHandler = null)
        {
            var exceptionHandler = optionalExceptionHandler;
            if (exceptionHandler == null)
            {
                exceptionHandler = e => Debug.WriteLine(e);
            }

            try
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
                _statsdData = _statsdBuilder.BuildStatsData(config, exceptionHandler);
                _metricsSender = _statsdData.MetricsSender;
            }
            catch (Exception e)
            {
                exceptionHandler.Invoke(e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Records an event.
        /// </summary>
        /// <param name="title">The title of the event.</param>
        /// <param name="text">The text body of the event.</param>
        /// <param name="alertType">error, warning, success, or info (defaults to info).</param>
        /// <param name="aggregationKey">A key to use for aggregating events.</param>
        /// <param name="sourceType">The source type name.</param>
        /// <param name="dateHappened">The epoch timestamp for the event (defaults to the current time from the DogStatsD server).</param>
        /// <param name="priority">Specifies the priority of the event (normal or low).</param>
        /// <param name="hostname">The name of the host.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        public void Event(string title, string text, string alertType = null, string aggregationKey = null, string sourceType = null, int? dateHappened = null, string priority = null, string hostname = null, string[] tags = null)
        {
            _metricsSender?.SendEvent(title, text, alertType, aggregationKey, sourceType, dateHappened, priority, hostname, tags);
        }

        /// <summary>
        /// Adjusts the specified counter by a given delta.
        /// </summary>
        /// <param name="statName">The name of the metric.</param>
        /// <param name="value">A given delta.</param>
        /// <param name="sampleRate">Percentage of metric to be sent.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        /// <param name="timestamp">BETA - Please contact our support team for more information to use this feature: https://www.datadoghq.com/support/ - Timestamp at which the counter has been seen with the given value. This value is sent without any aggregation.</param>
        public void Counter(string statName, double value, double sampleRate = 1.0, string[] tags = null, DateTimeOffset? timestamp = null)
        {
            _metricsSender?.SendMetric(MetricType.Count, statName, value, sampleRate, tags, timestamp);
        }

        /// <summary>
        /// Increments the specified counter.
        /// </summary>
        /// <param name="statName">The name of the metric.</param>
        /// <param name="value">The amount of increment.</param>
        /// <param name="sampleRate">Percentage of metric to be sent.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        /// <param name="timestamp">BETA - Please contact our support team for more information to use this feature: https://www.datadoghq.com/support/ - Timestamp at which the counter has been seen with the given value. This value is sent without any aggregation.</param>
        public void Increment(string statName, int value = 1, double sampleRate = 1.0, string[] tags = null, DateTimeOffset? timestamp = null)
        {
            _metricsSender?.SendMetric(MetricType.Count, statName, value, sampleRate, tags, timestamp);
        }

        /// <summary>
        /// Decrements the specified counter.
        /// </summary>
        /// <param name="statName">The name of the metric.</param>
        /// <param name="value">The amount of decrement.</param>
        /// <param name="sampleRate">Percentage of metric to be sent.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        /// <param name="timestamp">BETA - Please contact our support team for more information to use this feature: https://www.datadoghq.com/support/ - Timestamp at which the counter has been seen with the given value. This value is sent without any aggregation.</param>
        public void Decrement(string statName, int value = 1, double sampleRate = 1.0, string[] tags = null, DateTimeOffset? timestamp = null)
        {
            _metricsSender?.SendMetric(MetricType.Count, statName, -value, sampleRate, tags, timestamp);
        }

        /// <summary>
        /// Records the latest fixed value for the specified named gauge.
        /// </summary>
        /// <param name="statName">The name of the metric.</param>
        /// <param name="value">The value of the gauge.</param>
        /// <param name="sampleRate">Percentage of metric to be sent.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        /// <param name="timestamp">BETA - Please contact our support team for more information to use this feature: https://www.datadoghq.com/support/ - Timestamp at which the gauge has been seen with the given value. This value is sent without any aggregation.</param>
        public void Gauge(string statName, double value, double sampleRate = 1.0, string[] tags = null, DateTimeOffset? timestamp = null)
        {
            _metricsSender?.SendMetric(MetricType.Gauge, statName, value, sampleRate, tags, timestamp);
        }

        /// <summary>
        /// Records a value for the specified named histogram.
        /// </summary>
        /// <param name="statName">The name of the metric.</param>
        /// <param name="value">The value of the histogram.</param>
        /// <param name="sampleRate">Percentage of metric to be sent.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        public void Histogram(string statName, double value, double sampleRate = 1.0, string[] tags = null)
        {
            _metricsSender?.SendMetric(MetricType.Histogram, statName, value, sampleRate, tags, null);
        }

        /// <summary>
        /// Records a value for the specified named distribution.
        /// </summary>
        /// <param name="statName">The name of the metric.</param>
        /// <param name="value">The value of the distribution.</param>
        /// <param name="sampleRate">Percentage of metric to be sent.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        public void Distribution(string statName, double value, double sampleRate = 1.0, string[] tags = null)
        {
            _metricsSender?.SendMetric(MetricType.Distribution, statName, value, sampleRate, tags, null);
        }

        /// <summary>
        /// Records a value for the specified set.
        /// </summary>
        /// <param name="statName">The name of the metric.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="sampleRate">Percentage of metric to be sent.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        public void Set<T>(string statName, T value, double sampleRate = 1.0, string[] tags = null)
        {
            var strValue = string.Format(CultureInfo.InvariantCulture, "{0}", value);
            _metricsSender?.SendSetMetric(statName, strValue, sampleRate, tags);
        }

        /// <summary>
        /// Records a value for the specified set.
        /// </summary>
        /// <param name="statName">The name of the metric.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="sampleRate">Percentage of metric to be sent.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        public void Set(string statName, string value, double sampleRate = 1.0, string[] tags = null)
        {
            _metricsSender?.SendSetMetric(statName, value, sampleRate, tags);
        }

        /// <summary>
        /// Records an execution time in milliseconds.
        /// </summary>
        /// <param name="statName">The name of the metric.</param>
        /// <param name="value">The time in millisecond.</param>
        /// <param name="sampleRate">Percentage of metric to be sent.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        public void Timer(string statName, double value, double sampleRate = 1.0, string[] tags = null)
        {
            _metricsSender?.SendMetric(MetricType.Timing, statName, value, sampleRate, tags, null);
        }

        /// <summary>
        /// Creates a timer that records the execution time until Dispose is called on the returned value.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="sampleRate">Percentage of metric to be sent.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        /// <returns>A disposable object that records the execution time until Dispose is called.</returns>
        public IDisposable StartTimer(string name, double sampleRate = 1.0, string[] tags = null)
        {
            return new MetricsTimer(this, name, sampleRate, tags);
        }

        /// <summary>
        /// Records an execution time for the given action.
        /// </summary>
        /// <param name="action">The given action.</param>
        /// <param name="statName">The name of the metric.</param>
        /// <param name="sampleRate">Percentage of metric to be sent.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        public void Time(Action action, string statName, double sampleRate = 1.0, string[] tags = null)
        {
            if (_metricsSender == null)
            {
                action();
            }
            else
            {
                _metricsSender.Send(action, statName, sampleRate, tags);
            }
        }

        /// <summary>
        /// Records an execution time for the given function.
        /// </summary>
        /// <param name="func">The given function.</param>
        /// <param name="statName">The name of the metric.</param>
        /// <param name="sampleRate">Percentage of metric to be sent.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        /// <typeparam name="T">The type of the returned value of <paramref name="func"/>.</typeparam>
        /// <returns>The returned value of <paramref name="func"/>.</returns>
        public T Time<T>(Func<T> func, string statName, double sampleRate = 1.0, string[] tags = null)
        {
            if (_metricsSender == null)
            {
                return func();
            }

            using (StartTimer(statName, sampleRate, tags))
            {
                return func();
            }
        }

        /// <summary>
        /// Records a run status for the specified named service check.
        /// </summary>
        /// <param name="name">The name of the service check.</param>
        /// <param name="status">A constant describing the service status.</param>
        /// <param name="timestamp">The epoch timestamp for the service check (defaults to the current time from the DogStatsD server).</param>
        /// <param name="hostname">The hostname to associate with the service check.</param>
        /// <param name="tags">Array of tags to be added to the data.</param>
        /// <param name="message">Additional information or a description of why the status occurred.</param>
        public void ServiceCheck(string name, Status status, int? timestamp = null, string hostname = null, string[] tags = null, string message = null)
        {
            _metricsSender?.SendServiceCheck(name, (int)status, timestamp, hostname, tags, message);
        }

        /// <summary>
        /// Flushes all metrics.
        /// </summary>
        /// <param name="flushTelemetry">The value indicating whether the telemetry must be flushed.</param>
        public void Flush(bool flushTelemetry = true)
        {
            _statsdData?.Flush(flushTelemetry);
        }

        /// <summary>
        /// Disposes an instance of DogStatsdService.
        /// Flushes all metrics.
        /// </summary>
        public void Dispose()
        {
            _statsdData?.Dispose();
            _statsdData = null;
        }
    }
}
