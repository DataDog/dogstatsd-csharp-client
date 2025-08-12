using System;
using System.Collections.Generic;
using StatsdClient.Bufferize;
using StatsdClient.Statistic;

namespace StatsdClient.Aggregator
{
    /// <summary>
    /// AggregatorFlusher is responsible for flushing the aggregated `MetricStats` instances.
    /// </summary>
    internal class AggregatorFlusher<T>
    {
        private readonly MetricSerializer _serializer;
        private readonly BufferBuilder _bufferBuilder;
        private readonly Dictionary<MetricStatsKey, T> _values = new Dictionary<MetricStatsKey, T>();
        private readonly System.Diagnostics.Stopwatch _stopWatch = System.Diagnostics.Stopwatch.StartNew();
        private readonly int _maxUniqueStatsBeforeFlush;
        private readonly long _flushIntervalMilliseconds;
        private readonly SerializedMetric _serializedMetric = new SerializedMetric();
        private readonly MetricType _expectedMetricType;
        private readonly Action<T> _flushMetric;
        private readonly Telemetry _optionalTelemetry;

        public AggregatorFlusher(
            MetricAggregatorParameters parameters,
            MetricType expectedMetricType,
            Action<AggregatorFlusher<T>, T> flushMetric)
        {
            _serializer = parameters.Serializer;
            _bufferBuilder = parameters.BufferBuilder;
            _flushIntervalMilliseconds = (long)parameters.FlushInterval.TotalMilliseconds;
            _maxUniqueStatsBeforeFlush = parameters.MaxUniqueStatsBeforeFlush;
            _optionalTelemetry = parameters.OptionalTelemetry;
            _expectedMetricType = expectedMetricType;
            _flushMetric = v => flushMetric(this, v);
        }

        public bool TryGetValue(ref MetricStatsKey key, out T v)
        {
            return this._values.TryGetValue(key, out v);
        }

        public void Add(ref MetricStatsKey key, T v)
        {
            this._values.Add(key, v);
        }

        public void Update(ref MetricStatsKey key, T v)
        {
            this._values[key] = v;
        }

        public void TryFlush(bool force)
        {
            if (force
            || _stopWatch.ElapsedMilliseconds > _flushIntervalMilliseconds
            || _values.Count >= _maxUniqueStatsBeforeFlush)
            {
                foreach (var keyValue in _values)
                {
                    _flushMetric(keyValue.Value);
                }

                _bufferBuilder.HandleBufferAndReset();
                _optionalTelemetry?.OnAggregatedContextFlush(_expectedMetricType, _values.Count);
                this._stopWatch.Restart();
                _values.Clear();
            }
        }

        public void FlushStatsMetric(StatsMetric metric)
        {
            _serializer.SerializeTo(ref metric, _serializedMetric);
            _bufferBuilder.Add(_serializedMetric);
        }

        public MetricStatsKey CreateKey(StatsMetric metric)
        {
            if (metric.MetricType != _expectedMetricType)
            {
                throw new ArgumentException($"Metric type is {metric.MetricType} instead of {_expectedMetricType}.");
            }

            return new MetricStatsKey(metric.StatName, metric.Tags, metric.Cardinality);
        }
    }
}
