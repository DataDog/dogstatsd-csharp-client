using System;
using System.Globalization;

namespace StatsdClient
{
    internal class EventSerializer
    {
        private const int MaxSize = 8 * 1024;
        private readonly string[] _constantTags;

        public EventSerializer(string[] constantTags)
        {
            _constantTags = constantTags;
        }

        public SerializedMetric GetCommand(string title, string text, string alertType, string aggregationKey, string sourceType, int? dateHappened, string priority, string hostname, string[] tags, bool truncateIfTooLong = false)
        {
            return Serialize(title, text, alertType, aggregationKey, sourceType, dateHappened, priority, hostname, tags, truncateIfTooLong);
        }

        public SerializedMetric Serialize(string title, string text, string alertType, string aggregationKey, string sourceType, int? dateHappened, string priority, string hostname, string[] tags, bool truncateIfTooLong = false)
        {
            string processedTitle = SerializerHelper.EscapeContent(title);
            string processedText = SerializerHelper.EscapeContent(text);
            string result = string.Format(CultureInfo.InvariantCulture, "_e{{{0},{1}}}:{2}|{3}", processedTitle.Length.ToString(), processedText.Length.ToString(), processedTitle, processedText);
            if (dateHappened != null)
            {
                result += string.Format(CultureInfo.InvariantCulture, "|d:{0}", dateHappened);
            }

            if (hostname != null)
            {
                result += string.Format(CultureInfo.InvariantCulture, "|h:{0}", hostname);
            }

            if (aggregationKey != null)
            {
                result += string.Format(CultureInfo.InvariantCulture, "|k:{0}", aggregationKey);
            }

            if (priority != null)
            {
                result += string.Format(CultureInfo.InvariantCulture, "|p:{0}", priority);
            }

            if (sourceType != null)
            {
                result += string.Format(CultureInfo.InvariantCulture, "|s:{0}", sourceType);
            }

            if (alertType != null)
            {
                result += string.Format(CultureInfo.InvariantCulture, "|t:{0}", alertType);
            }

            result += MetricSerializer.ConcatTags(_constantTags, tags);

            if (result.Length > MaxSize)
            {
                if (truncateIfTooLong)
                {
                    var overage = result.Length - MaxSize;
                    if (title.Length > text.Length)
                    {
                        title = SerializerHelper.TruncateOverage(title, overage);
                    }
                    else
                    {
                        text = SerializerHelper.TruncateOverage(text, overage);
                    }

                    return GetCommand(title, text, alertType, aggregationKey, sourceType, dateHappened, priority, hostname, tags, true);
                }
                else
                {
                    throw new Exception(string.Format("Event {0} payload is too big (more than 8kB)", title));
                }
            }

            return new SerializedMetric(result);
        }
    }
}