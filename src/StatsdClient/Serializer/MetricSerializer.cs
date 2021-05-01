using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using StatsdClient.Statistic;

namespace StatsdClient
{
    internal class MetricSerializer
    {
        private static readonly Dictionary<MetricType, string> _commandToUnit = new Dictionary<MetricType, string>
                                                                {
                                                                    { MetricType.Count, "c" },
                                                                    { MetricType.Timing, "ms" },
                                                                    { MetricType.Gauge, "g" },
                                                                    { MetricType.Histogram, "h" },
                                                                    { MetricType.Distribution, "d" },
                                                                    { MetricType.Meter, "m" },
                                                                    { MetricType.Set, "s" },
                                                                };

        private readonly SerializerHelper _serializerHelper;
        private readonly string _prefix;
        private readonly char[] doubleBuffer = new char[32];

        internal MetricSerializer(SerializerHelper serializerHelper, string prefix)
        {
            _serializerHelper = serializerHelper;
            _prefix = string.IsNullOrEmpty(prefix) ? string.Empty : prefix + ".";
        }

        public void SerializeTo(ref StatsMetric metricStats, SerializedMetric serializedMetric)
        {
            serializedMetric.Reset();

            var builder = serializedMetric.Builder;
            var unit = _commandToUnit[metricStats.MetricType];

            builder.Append(_prefix);
            builder.Append(metricStats.StatName);
            builder.Append(':');
            switch (metricStats.MetricType)
            {
                case MetricType.Set: builder.Append(metricStats.StringValue); break;
                default: AppendDouble(builder, metricStats.NumericValue); break;
            }

            builder.Append('|');
            builder.Append(unit);

            if (metricStats.SampleRate != 1.0)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, "|@{0}", metricStats.SampleRate);
            }

            _serializerHelper.AppendTags(builder, metricStats.Tags);
        }

        private void AppendDouble(StringBuilder builder, double v)
        {
#if NETSTANDARD2_1
            Span<char> span = doubleBuffer;
            if (v.TryFormat(span, out int charsWritten, provider: CultureInfo.InvariantCulture))
            {
                builder.Append(doubleBuffer, 0, charsWritten);
                return;
            }
#endif
            builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", v);
        }
    }
}
