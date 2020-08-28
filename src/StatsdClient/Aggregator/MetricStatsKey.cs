using System.Collections.Generic;

namespace StatsdClient.Aggregator
{
    /// <summary>
    /// Dictionary Key for `MetricStats`.
    /// It is more efficient to use `MetricStatsKey` than creating a string from metric name and tags.
    /// </summary>
    internal struct MetricStatsKey
    {
        private readonly string _metricName;
        private readonly string[] _tags;

        public MetricStatsKey(string metricName, string[] tags)
        {
            _metricName = metricName;
            _tags = tags;
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