using System;
using System.Collections.Generic;
using System.Threading;
using StatsdClient.Bufferize;
using StatsdClient.Statistic;
using StatsdClient.Utils;

namespace StatsdClient
{
    class Data
    {
        public StatsBufferize _statsBufferizes;
        public Pool<Stats> _pool;
    }
    internal class MetricsSender
    {
        private readonly Telemetry _optionalTelemetry;
        private readonly List<Data> _data;
        private readonly bool _truncateIfTooLong;
        private readonly IStopWatchFactory _stopwatchFactory;
        private readonly IRandomGenerator _randomGenerator;
        private readonly Action onMetricSent;

        internal MetricsSender(
                    Func<StatsBufferize> statsBufferize,
                    IRandomGenerator randomGenerator,
                    IStopWatchFactory stopwatchFactory,
                    Telemetry optionalTelemetry,
                    bool truncateIfTooLong,
                    int poolMaxAllocation)
        {
            _stopwatchFactory = stopwatchFactory;
            _data = new List<Data>();
            for (int i = 0; i < 4; ++i)
            {
                _data.Add(new Data
                {
                    _statsBufferizes = statsBufferize(),
                    _pool = new Pool<Stats>(pool => new Stats(pool), poolMaxAllocation)
                });
            }

            _randomGenerator = randomGenerator;
            _optionalTelemetry = optionalTelemetry;
            _truncateIfTooLong = truncateIfTooLong;
            onMetricSent = () => _optionalTelemetry?.OnMetricSent();
        }

        public void SendEvent(string title, string text, string alertType = null, string aggregationKey = null, string sourceType = null, int? dateHappened = null, string priority = null, string hostname = null, string[] tags = null, bool truncateIfTooLong = false)
        {
            if (TryDequeueStats(null, out var stats))
            {
                stats.Kind = StatsKind.Event;
                stats.Event.Tags = tags;
                stats.Event.Title = title;
                stats.Event.Text = text;
                stats.Event.AlertType = alertType;
                stats.Event.AggregationKey = aggregationKey;
                stats.Event.SourceType = sourceType;
                stats.Event.DateHappened = dateHappened;
                stats.Event.Priority = priority;
                stats.Event.Hostname = hostname;
                stats.Event.TruncateIfTooLong = truncateIfTooLong || _truncateIfTooLong;

                Send(stats, () => _optionalTelemetry?.OnEventSent(), null);
            }
        }

        public void SendServiceCheck(string name, int status, int? timestamp = null, string hostname = null, string[] tags = null, string serviceCheckMessage = null, bool truncateIfTooLong = false)
        {
            if (TryDequeueStats(null, out var stats))
            {
                stats.Kind = StatsKind.ServiceCheck;
                stats.ServiceCheck.Tags = tags;
                stats.ServiceCheck.Name = name;
                stats.ServiceCheck.Status = status;
                stats.ServiceCheck.Timestamp = timestamp;
                stats.ServiceCheck.Hostname = hostname;
                stats.ServiceCheck.ServiceCheckMessage = serviceCheckMessage;
                stats.ServiceCheck.TruncateIfTooLong = truncateIfTooLong || _truncateIfTooLong;
                Send(stats, () => _optionalTelemetry?.OnServiceCheckSent(), null);
            }
        }

        public void SendMetric(MetricType metricType, string name, double value, double sampleRate = 1.0, string[] tags = null)
        {
            if (metricType == MetricType.Set)
            {
                throw new ArgumentException($"{nameof(SendMetric)} does not support `MetricType.Set`.");
            }

            if (sampleRate == 1.0 || _randomGenerator.ShouldSend(sampleRate))
            {
                var data = GetData();
                if (TryDequeueStats(data, out var stats))
                {
                    stats.Kind = StatsKind.Metric;
                    stats.Metric.Tags = tags;
                    stats.Metric.MetricType = metricType;
                    stats.Metric.StatName = name;
                    stats.Metric.SampleRate = sampleRate;
                    stats.Metric.NumericValue = value;

                    Send(stats, onMetricSent, data._statsBufferizes);
                }
            }
        }

        public void SendSetMetric(string name, string value, double sampleRate = 1.0, string[] tags = null)
        {
            if (_randomGenerator.ShouldSend(sampleRate))
            {
                var data = GetData();
                if (TryDequeueStats(data, out var stats))
                {
                    stats.Kind = StatsKind.Metric;
                    stats.Metric.Tags = tags;
                    stats.Metric.MetricType = MetricType.Set;
                    stats.Metric.StatName = name;
                    stats.Metric.SampleRate = sampleRate;
                    stats.Metric.StringValue = value;

                    Send(stats, () => _optionalTelemetry?.OnMetricSent(), data._statsBufferizes
                    );
                }
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

        private bool TryDequeueStats(Data data, out Stats stats)
        {
            if (data._pool.TryDequeue(out stats))
            {
                return true;
            }

            _optionalTelemetry?.OnPacketsDroppedQueue();
            return false;
        }

        int i = 0;

        private Data GetData()
        {
            var index = Interlocked.Increment(ref i) % _data.Count;

            return _data[index];
        }

        private void Send(Stats metricFields, Action onSuccess, StatsBufferize statsBufferizes)
        {
            if (statsBufferizes.Send(metricFields))
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
