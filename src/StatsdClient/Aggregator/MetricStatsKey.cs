using System.Collections.Generic;
using StatsdClient.Statistic;

namespace StatsdClient.Aggregator
{
    /// <summary>
    /// Dictionary Key for `MetricStats`.
    /// It is more efficient to use `MetricStatsKey` than creating a string from metric name and tags.
    /// </summary>
    internal class MetricStatsKey
    {
        private string _metricName => _metric.StatName;
        private string[] _tags => _metric.Tags;
        private readonly StatsMetric _metric;

        public MetricStatsKey(StatsMetric metric)
        {            
            _metric = metric;
        }

        // This code was auto generated
        public override bool Equals(object obj)
        {
            return obj is MetricStatsKey key &&
                   _metricName == key._metricName &&
                   EqualityComparer<string[]>.Default.Equals(_tags, key._tags);
        }

        // This code was auto generated
        public override int GetHashCode()
        {
            int hashCode = -335110880;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(_metricName);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string[]>.Default.GetHashCode(_tags);
            return hashCode;
        }
    }
}