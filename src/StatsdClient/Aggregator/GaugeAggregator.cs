using StatsdClient.Statistic;

namespace StatsdClient.Aggregator
{
    /// <summary>
    /// Aggregate `StatsMetric` instances of type `Gauge` by by keeping the last value.
    /// </summary>
    internal class GaugeAggregator
    {
        private readonly AggregatorFlusher<StatsMetric> _aggregator;

        public GaugeAggregator(MetricAggregatorParameters parameters)
        {
            _aggregator = new AggregatorFlusher<StatsMetric>(parameters, MetricType.Gauge);
        }

        public void OnNewValue(StatsMetric metric)
        {
            var key = _aggregator.CreateKey(metric);
            if (_aggregator.TryGetValue(key, out var _))
            {
                _aggregator.Update(key, metric);
            }
            else
            {
                _aggregator.Add(key, metric);
            }

            this.TryFlush();
        }

        public void TryFlush(bool force = false)
        {
            _aggregator.TryFlush(
                values =>
                {
                    foreach (var keyValue in values)
                    {
                        _aggregator.FlushStatsMetric(keyValue.Value);
                    }
                },
                force);
        }
    }
}