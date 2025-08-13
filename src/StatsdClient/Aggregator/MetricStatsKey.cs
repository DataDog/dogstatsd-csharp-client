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
        private readonly Cardinality? _cardinality;

        public MetricStatsKey(string metricName, string[] tags, Cardinality? cardinality = null)
        {
            _metricName = metricName;
            _tags = tags;
            _cardinality = cardinality;
        }

        public override bool Equals(object obj)
        {
            return obj is MetricStatsKey key
                && _metricName == key._metricName
                && AreEquals(_tags, key._tags)
                && _cardinality == key._cardinality;
        }

        public override int GetHashCode()
        {
            int hashCode = -335110880;
            hashCode = (hashCode * -1521134295) + _metricName.GetHashCode();
            if (_tags != null)
            {
                foreach (var tag in _tags)
                {
                    hashCode = (hashCode * -1521134295) + tag.GetHashCode();
                }
            }

            hashCode = (hashCode * -1521134295) + _cardinality.GetHashCode();

            return hashCode;
        }

        private static bool AreEquals(string[] arr1, string[] arr2)
        {
            if (arr1 == null && arr2 == null)
            {
                return true;
            }

            if (arr1 == null || arr2 == null || arr1.Length != arr2.Length)
            {
                return false;
            }

            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
