using System;
using StatsdClient.Bufferize;
using StatsdClient.Statistic;

namespace StatsdClient
{
    internal class MetricsSender
    {
        private readonly Telemetry _optionalTelemetry;
        private readonly StatsBufferize _statsBufferize;
        private readonly bool _truncateIfTooLong;
        private readonly IStopWatchFactory _stopwatchFactory;
        private readonly IRandomGenerator _randomGenerator;

        internal MetricsSender(
                    StatsBufferize statsBufferize,
                    IRandomGenerator randomGenerator,
                    IStopWatchFactory stopwatchFactory,
                    Telemetry optionalTelemetry,
                    bool truncateIfTooLong)
        {
            _stopwatchFactory = stopwatchFactory;
            _statsBufferize = statsBufferize;
            _randomGenerator = randomGenerator;
            _optionalTelemetry = optionalTelemetry;
            _truncateIfTooLong = truncateIfTooLong;
        }

        public void SendEvent(string title, string text, string alertType = null, string aggregationKey = null, string sourceType = null, int? dateHappened = null, string priority = null, string hostname = null, string[] tags = null, bool truncateIfTooLong = false)
        {
            var m = new Stats();
            m.Kind = StatsKind.Event;
            m.Tags = tags;
            m.Event.Title = title;
            m.Event.Text = text;
            m.Event.AlertType = alertType;
            m.Event.AggregationKey = aggregationKey;
            m.Event.SourceType = sourceType;
            m.Event.DateHappened = dateHappened;
            m.Event.Priority = priority;
            m.Event.Hostname = hostname;
            m.Event.TruncateIfTooLong = truncateIfTooLong || _truncateIfTooLong;

            Send(m, () => _optionalTelemetry?.OnEventSent());
        }

        public void SendServiceCheck(string name, int status, int? timestamp = null, string hostname = null, string[] tags = null, string serviceCheckMessage = null, bool truncateIfTooLong = false)
        {
            var m = new Stats();
            m.Kind = StatsKind.ServiceCheck;
            m.Tags = tags;
            m.ServiceCheck.Name = name;
            m.ServiceCheck.Status = status;
            m.ServiceCheck.Timestamp = timestamp;
            m.ServiceCheck.Hostname = hostname;
            m.ServiceCheck.ServiceCheckMessage = serviceCheckMessage;
            m.ServiceCheck.TruncateIfTooLong = truncateIfTooLong || _truncateIfTooLong;
            Send(m, () => _optionalTelemetry?.OnServiceCheckSent());
        }

        public void SendMetric(MetricType metricType, string name, double value, double sampleRate = 1.0, string[] tags = null)
        {
            if (metricType == MetricType.Set)
            {
                throw new ArgumentException($"{nameof(SendMetric)} does not support `MetricType.Set`.");
            }

            if (_randomGenerator.ShouldSend(sampleRate))
            {
                var m = new Stats();
                m.Kind = StatsKind.Metric;
                m.Tags = tags;
                m.Metric.MetricType = metricType;
                m.Metric.StatName = name;
                m.Metric.SampleRate = sampleRate;
                m.Metric.NumericValue = value;

                Send(m, () => _optionalTelemetry?.OnMetricSent());
            }
        }

        public void SendSetMetric(string name, string value, double sampleRate = 1.0, string[] tags = null)
        {
            if (_randomGenerator.ShouldSend(sampleRate))
            {
                var m = new Stats();
                m.Kind = StatsKind.Metric;
                m.Tags = tags;
                m.Metric.MetricType = MetricType.Set;
                m.Metric.StatName = name;
                m.Metric.SampleRate = sampleRate;
                m.Metric.StringValue = value;

                Send(m, () => _optionalTelemetry?.OnMetricSent());
            }
        }

        public void Send(Action actionToTime, string statName, double sampleRate = 1.0, string[] tags = null)
        {
            var stopwatch = _stopwatchFactory.Get();

            try
            {
                stopwatch.Start();
                actionToTime();
            }
            finally
            {
                stopwatch.Stop();
                SendMetric(MetricType.Timing, statName, stopwatch.ElapsedMilliseconds(), sampleRate, tags);
            }
        }

        private void Send(Stats optionalMetricFields, Action onSuccess)
        {
            if (optionalMetricFields != null && _statsBufferize.Send(optionalMetricFields))
            {
                onSuccess();
            }
            else
            {
                _optionalTelemetry?.OnPacketsDroppedQueue();
            }
        }
    }
}
