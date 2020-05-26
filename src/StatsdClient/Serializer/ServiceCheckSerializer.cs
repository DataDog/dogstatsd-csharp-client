using System;
using System.Globalization;

namespace StatsdClient
{
    internal class ServiceCheckSerializer
    {
        private const int MaxSize = 8 * 1024;

        public static string GetCommand(string name, int status, int? timestamp, string hostname, string[] tags, string serviceCheckMessage, bool truncateIfTooLong = false)
        {
            return GetCommand(name, status, timestamp, hostname, null, tags, serviceCheckMessage, truncateIfTooLong);
        }

        public static string GetCommand(string name, int status, int? timestamp, string hostname, string[] constantTags, string[] tags, string serviceCheckMessage, bool truncateIfTooLong = false)
        {
            string processedName = EscapeName(name);
            string processedMessage = EscapeMessage(serviceCheckMessage);

            string result = string.Format(CultureInfo.InvariantCulture, "_sc|{0}|{1}", processedName, status);

            if (timestamp != null)
            {
                result += string.Format(CultureInfo.InvariantCulture, "|d:{0}", timestamp);
            }

            if (hostname != null)
            {
                result += string.Format(CultureInfo.InvariantCulture, "|h:{0}", hostname);
            }

            result += MetricSerializer.ConcatTags(constantTags, tags);

            // Note: this must always be appended to the result last.
            if (processedMessage != null)
            {
                result += string.Format(CultureInfo.InvariantCulture, "|m:{0}", processedMessage);
            }

            if (result.Length > MaxSize)
            {
                if (!truncateIfTooLong)
                {
                    throw new Exception(string.Format("ServiceCheck {0} payload is too big (more than 8kB)", name));
                }

                var overage = result.Length - MaxSize;

                if (processedMessage == null || overage > processedMessage.Length)
                {
                    throw new ArgumentException(string.Format("ServiceCheck name is too long to truncate, payload is too big (more than 8Kb) for {0}", name), "name");
                }

                var truncMessage = MetricSerializer.TruncateOverage(processedMessage, overage);
                return GetCommand(name, status, timestamp, hostname, tags, truncMessage, true);
            }

            return result;
        }

        // Service check name string, shouldn’t contain any |
        private static string EscapeName(string name)
        {
            name = MetricSerializer.EscapeContent(name);

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
                return MetricSerializer.EscapeContent(message).Replace("m:", "m\\:");
            }

            return message;
        }
    }
}