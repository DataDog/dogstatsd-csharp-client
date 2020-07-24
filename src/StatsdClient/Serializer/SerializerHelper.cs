using System.Text;
using StatsdClient.Utils;

namespace StatsdClient
{
    internal class SerializerHelper
    {
        private static readonly string[] EmptyArray = new string[0];
        private readonly Pool<SerializedMetric> _pool;
        private readonly string _constantTags;

        public SerializerHelper(string[] constantTags)
        {
            _constantTags = constantTags != null ? string.Join(",", constantTags) : string.Empty;
            _pool = new Pool<SerializedMetric>(pool => new SerializedMetric(pool), 100000);
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

        public SerializedMetric GetOptionalSerializedMetric()
        {
            _pool.TryDequeue(out var serializedMetric);
            return serializedMetric;
        }

        public void AppendTags(StringBuilder builder, string[] tags)
        {
            if (tags == null)
            {
                tags = EmptyArray;
            }

            bool hasConstantTags = !string.IsNullOrEmpty(_constantTags);

            if (hasConstantTags || tags.Length > 0)
            {
                bool tagAppened = false;
                builder.Append("|#");
                if (hasConstantTags)
                {
                    builder.Append(_constantTags);
                    tagAppened = true;
                }

                // Do not use String.Join to avoid a memory allocation.
                foreach (var tag in tags)
                {
                    if (tagAppened)
                    {
                        builder.Append(',');
                    }

                    tagAppened = true;
                    builder.Append(tag);
                }
            }
        }
    }
}
