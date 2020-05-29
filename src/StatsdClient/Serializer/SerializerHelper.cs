using System.Text;

namespace StatsdClient
{
    internal class SerializerHelper
    {
        private readonly string _constantTags;

        public SerializerHelper(string[] constantTags)
        {
            _constantTags = constantTags != null ? string.Join(",", constantTags) : string.Empty;
        }

        public static string EscapeContent(string content)
        {
            return content
                .Replace("\r", string.Empty)
                .Replace("\n", "\\n");
        }

        public static string TruncateOverage(string str, int overage)
        {
            return str.Substring(0, str.Length - overage);
        }

        public static void AppendIfNotNull(StringBuilder builder, string prefix, string value)
        {
            if (value != null)
            {
                builder.Append(prefix);
                builder.Append(value);
            }
        }

        public SerializedMetric GetRawMetric()
        {
            return new SerializedMetric(string.Empty);
        }

        public void AppendTags(StringBuilder builder, string[] tags)
        {
            if (!string.IsNullOrEmpty(_constantTags) || (tags != null && tags.Length > 0))
            {
                builder.Append("|#");
                builder.Append(_constantTags);
                bool hasTag = !string.IsNullOrEmpty(_constantTags);
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        if (hasTag)
                        {
                            builder.Append(',');
                        }

                        hasTag = true;
                        builder.Append(tag);
                    }
                }
            }
        }
    }
}
