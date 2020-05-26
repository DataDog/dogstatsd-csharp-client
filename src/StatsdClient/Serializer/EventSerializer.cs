using System;
using System.Globalization;

namespace StatsdClient
{
    internal class EventSerializer
    {
        private const int MaxSize = 8 * 1024;

        public static string GetCommand(string title, string text, string alertType, string aggregationKey, string sourceType, int? dateHappened, string priority, string hostname, string[] tags, bool truncateIfTooLong = false)
        {
            return GetCommand(title, text, alertType, aggregationKey, sourceType, dateHappened, priority, hostname, null, tags, truncateIfTooLong);
        }

        public static string GetCommand(string title, string text, string alertType, string aggregationKey, string sourceType, int? dateHappened, string priority, string hostname, string[] constantTags, string[] tags, bool truncateIfTooLong = false)
        {
            string processedTitle = MetricSerializer.EscapeContent(title);
            string processedText = MetricSerializer.EscapeContent(text);
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

            result += MetricSerializer.ConcatTags(constantTags, tags);

            if (result.Length > MaxSize)
            {
                if (truncateIfTooLong)
                {
                    var overage = result.Length - MaxSize;
                    if (title.Length > text.Length)
                    {
                        title = MetricSerializer.TruncateOverage(title, overage);
                    }
                    else
                    {
                        text = MetricSerializer.TruncateOverage(text, overage);
                    }

                    return GetCommand(title, text, alertType, aggregationKey, sourceType, dateHappened, priority, hostname, tags, true);
                }
                else
                {
                    throw new Exception(string.Format("Event {0} payload is too big (more than 8kB)", title));
                }
            }

            return result;
        }
    }
}