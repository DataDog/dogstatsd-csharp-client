using StatsdClient.Statistic;

namespace StatsdClient.Aggregator
{
    /// <summary>
    /// Aggregate <see cref="StatsMetric"/> instances of type <see cref="MetricType.Count"/>
    /// by summing the value by <see cref="MetricStatsKey"/>.
    /// </summary>
    internal class CountAggregator
    {
        private readonly AggregatorFlusher<StatsMetric> _aggregator;

        public CountAggregator(MetricAggregatorParameters parameters)
        {
            _aggregator = new AggregatorFlusher<StatsMetric>(parameters, MetricType.Count);
        }

        public void OnNewValue(ref StatsMetric metric)
        {
            var key = _aggregator.CreateKey(metric);
            if (_aggregator.TryGetValue(ref key, out var v))
            {
                v.NumericValue += metric.NumericValue;
                _aggregator.Update(ref key, v);
            }
            else
            {
                _aggregator.Add(ref key, metric);
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