using System.Text;

namespace StatsdClient
{
    internal class SerializedMetric
    {
        public SerializedMetric(string str)
        {
            Builder.Append(str);
        }

        public StringBuilder Builder { get; } = new StringBuilder();

        public override string ToString()
        {
            return Builder.ToString();
        }
    }
}