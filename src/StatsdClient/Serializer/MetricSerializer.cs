using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StatsdClient
{
    internal class MetricSerializer
    {
        private static readonly string[] EmptyStringArray = new string[0];
        private readonly string _prefix;
        private readonly string[] _constantTags;

        internal MetricSerializer(string prefix, string[] constantTags)
        {
            _prefix = string.IsNullOrEmpty(prefix) ? string.Empty : prefix + ".";
            // copy array to prevent changes, coalesce to empty array
            _constantTags = constantTags?.ToArray() ?? EmptyStringArray;
        }

        public static string EscapeContent(string content)
        {
            return content
                .Replace("\r", string.Empty)
                .Replace("\n", "\\n");
        }

        public static string ConcatTags(string[] constantTags, string[] tags)
        {
            // avoid dealing with null arrays
            constantTags = constantTags ?? EmptyStringArray;
            tags = tags ?? EmptyStringArray;

            if (constantTags.Length == 0 && tags.Length == 0)
            {
                return string.Empty;
            }

            var allTags = constantTags.Concat(tags);
            string concatenatedTags = string.Join(",", allTags);
            return $"|#{concatenatedTags}";
        }

        public static string TruncateOverage(string str, int overage)
        {
            return str.Substring(0, str.Length - overage);
        }

        public string SerializeEvent(string title, string text, string alertType = null, string aggregationKey = null, string sourceType = null, int? dateHappened = null, string priority = null, string hostname = null, string[] tags = null, bool truncateIfTooLong = false)
        {
            return EventSerializer.GetCommand(title, text, alertType, aggregationKey, sourceType, dateHappened, priority, hostname, _constantTags, tags, truncateIfTooLong);
        }

        public string SerializeServiceCheck(string name, int status, int? timestamp = null, string hostname = null, string[] tags = null, string serviceCheckMessage = null, bool truncateIfTooLong = false)
        {
            return ServiceCheckSerializer.GetCommand(name, status, timestamp, hostname, _constantTags, tags, serviceCheckMessage, truncateIfTooLong);
        }

        public string SerializeMetric<T>(MetricType metricType, string name, T value, double sampleRate = 1.0, string[] tags = null)
        {
            return Metric.GetCommand(metricType, _prefix, name, value, sampleRate, _constantTags, tags);
        }

        public abstract class Metric
        {
            private static readonly Dictionary<MetricType, string> _commandToUnit = new Dictionary<MetricType, string>
                                                                {
                                                                    { MetricType.Counting, "c" },
                                                                    { MetricType.Timing, "ms" },
                                                                    { MetricType.Gauge, "g" },
                                                                    { MetricType.Histogram, "h" },
                                                                    { MetricType.Distribution, "d" },
                                                                    { MetricType.Meter, "m" },
                                                                    { MetricType.Set, "s" },
                                                                };

            public static string GetCommand<T>(MetricType metricType, string prefix, string name, T value, double sampleRate, string[] constantTags, string[] tags)
            {
                string full_name = prefix + name;
                string unit = _commandToUnit[metricType];
                var allTags = ConcatTags(constantTags, tags);

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1}|{2}{3}{4}",
                    full_name,
                    value,
                    unit,
                    sampleRate == 1.0 ? string.Empty : string.Format(CultureInfo.InvariantCulture, "|@{0}", sampleRate),
                    allTags);
            }
        }
    }
}
