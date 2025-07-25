using System;
using System.Globalization;
using System.Text;
using StatsdClient.Statistic;

namespace StatsdClient
{
    internal class EventSerializer
    {
        private const int MaxSize = 8 * 1024;
        private readonly SerializerHelper _serializerHelper;

        public EventSerializer(SerializerHelper serializerHelper)
        {
            _serializerHelper = serializerHelper;
        }

        public void SerializeTo(ref StatsEvent statsEvent, SerializedMetric serializedMetric)
        {
            serializedMetric.Reset();
            string processedTitle = SerializerHelper.EscapeContent(statsEvent.Title);
            string processedText = SerializerHelper.EscapeContent(statsEvent.Text);
            var builder = serializedMetric.Builder;

            builder.Append("_e{");
            builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", Encoding.UTF8.GetByteCount(processedTitle));
            builder.Append(',');
            builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", Encoding.UTF8.GetByteCount(processedText));
            builder.Append("}:");
            builder.Append(processedTitle);
            builder.Append('|');
            builder.Append(processedText);

            if (statsEvent.DateHappened != null)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, "|d:{0}", statsEvent.DateHappened.Value);
            }

            SerializerHelper.AppendIfNotNull(builder, "|h:", statsEvent.Hostname);
            SerializerHelper.AppendIfNotNull(builder, "|k:", statsEvent.AggregationKey);
            SerializerHelper.AppendIfNotNull(builder, "|p:", statsEvent.Priority);
            SerializerHelper.AppendIfNotNull(builder, "|s:", statsEvent.SourceType);
            SerializerHelper.AppendIfNotNull(builder, "|t:", statsEvent.AlertType);

            _serializerHelper.AppendTags(builder, statsEvent.Tags);

            _serializerHelper.AppendExternalData(builder);

            if (builder.Length > MaxSize)
            {
                if (statsEvent.TruncateIfTooLong)
                {
                    var overage = builder.Length - MaxSize;
                    if (statsEvent.Title.Length > statsEvent.Text.Length)
                    {
                        statsEvent.Title = SerializerHelper.TruncateOverage(statsEvent.Title, overage);
                    }
                    else
                    {
                        statsEvent.Text = SerializerHelper.TruncateOverage(statsEvent.Text, overage);
                    }

                    statsEvent.TruncateIfTooLong = true;
                    SerializeTo(ref statsEvent, serializedMetric);
                }
                else
                {
                    throw new Exception(string.Format("Event {0} payload is too big (more than 8kB)", statsEvent.Title));
                }
            }
        }
    }
}
