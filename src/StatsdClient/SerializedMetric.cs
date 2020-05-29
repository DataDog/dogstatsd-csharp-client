using System.Collections.Concurrent;
using System.Text;

namespace StatsdClient
{
    internal class SerializedMetric
    {
        private readonly ConcurrentQueue<SerializedMetric> _pool;

        public SerializedMetric(ConcurrentQueue<SerializedMetric> pool)
        {
            _pool = pool;
        }

        public StringBuilder Builder { get; } = new StringBuilder();

        public override string ToString()
        {
            return Builder.ToString();
        }

        public void Dispose()
        {
            Builder.Clear();
            _pool.Enqueue(this);
        }
    }
}
