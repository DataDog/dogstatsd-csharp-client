using System;
using System.Globalization;
using System.Text;
using StatsdClient.Statistic;

namespace StatsdClient
{
    internal class ServiceCheckSerializer
    {
        private const int ServiceCheckMaxSize = 8 * 1024;
        private readonly SerializerHelper _serializerHelper;

        public ServiceCheckSerializer(SerializerHelper serializerHelper)
        {
            _serializerHelper = serializerHelper;
        }

        public void SerializeTo(ref StatsServiceCheck sc, SerializedMetric serializedMetric)
        {
            serializedMetric.Reset();

            var builder = serializedMetric.Builder;

            string processedName = EscapeName(sc.Name);
            string processedMessage = EscapeMessage(sc.ServiceCheckMessage);

            builder.Append("_sc|");
            builder.Append(processedName);
            builder.AppendFormat(CultureInfo.InvariantCulture, "|{0}", sc.Status);

            if (sc.Timestamp != null)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, "|d:{0}", sc.Timestamp.Value);
            }

            SerializerHelper.AppendIfNotNull(builder, "|h:", sc.Hostname);

            _serializerHelper.AppendTags(builder, sc.Tags);

            _serializerHelper.AppendExternalData(builder);

            // Note: this must always be appended to the result last.
            SerializerHelper.AppendIfNotNull(builder, "|m:", processedMessage);

            sc.ServiceCheckMessage = TruncateMessageIfRequired(sc.Name, builder, sc.TruncateIfTooLong, processedMessage);
            if (sc.ServiceCheckMessage != null)
            {
                sc.TruncateIfTooLong = true;
                SerializeTo(ref sc, serializedMetric);
            }
        }

        private static string TruncateMessageIfRequired(
            string name,
            StringBuilder builder,
            bool truncateIfTooLong,
            string processedMessage)
        {
            if (builder.Length > ServiceCheckMaxSize)
            {
                if (!truncateIfTooLong)
                {
                    throw new Exception(string.Format("ServiceCheck {0} payload is too big (more than 8kB)", name));
                }

                var overage = builder.Length - ServiceCheckMaxSize;

                if (processedMessage == null || overage > processedMessage.Length)
                {
                    throw new ArgumentException(string.Format("ServiceCheck name is too long to truncate, payload is too big (more than 8Kb) for {0}", name), "name");
                }

                return SerializerHelper.TruncateOverage(processedMessage, overage);
            }

            return null;
        }

        private static string EscapeName(string name)
        {
            name = SerializerHelper.EscapeContent(name);

            if (name.Contains("|"))
            {
                throw new ArgumentException("Name must not contain any | (pipe) characters", "name");
            }

            return name;
        }

        private static string EscapeMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                return SerializerHelper.EscapeContent(message).Replace("m:", "m\\:");
            }

            return message;
        }
    }
}
