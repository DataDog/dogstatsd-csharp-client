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
        private readonly char[] numericBuffer = new char[32];

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

            if (metricStats.Timestamp > 0)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, "|T{0}", metricStats.Timestamp);
            }

            _serializerHelper.AppendContainerID(builder);
            _serializerHelper.AppendExternalData(builder);

            SerializerHelper.AppendIfNotNull(builder, "|card:", metricStats.Cardinality?.ToString().ToLowerInvariant());
        }

        private void AppendDouble(StringBuilder builder, double v)
        {
            var intValue = (int)v;
            var provider = CultureInfo.InvariantCulture;

#if HAS_SPAN
            Span<char> span = numericBuffer;
            bool tryFormatSuccess;
            int charsWritten;

            // Try format as `int` as `v` is often an `int` value and formating an `int` is a lot faster.
            if (v == intValue)
            {
                tryFormatSuccess = intValue.TryFormat(span, out charsWritten, provider: provider);
            }
            else
            {
                tryFormatSuccess = v.TryFormat(span, out charsWritten, provider: provider);
            }

            if (tryFormatSuccess)
            {
                builder.Append(numericBuffer, 0, charsWritten);
                return;
            }
#endif

            if (v == intValue)
            {
                builder.AppendFormat(provider, "{0}", intValue);
            }
            else
            {
                builder.AppendFormat(provider, "{0}", v);
            }
        }
    }
}
