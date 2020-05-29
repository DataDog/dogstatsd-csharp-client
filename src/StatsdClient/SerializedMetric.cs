using System;
using System.Collections.Concurrent;
using System.Text;

namespace StatsdClient
{
    internal class SerializedMetric : IDisposable
    {
        private readonly ConcurrentQueue<SerializedMetric> _pool;

        public SerializedMetric(ConcurrentQueue<SerializedMetric> pool)
        {
            _pool = pool;
        }

        public StringBuilder Builder { get; } = new StringBuilder();

        public int CopyToChars(char[] charsBuffers)
        {
            var length = Builder.Length;
            if (length > charsBuffers.Length)
            {
                return -1;
            }

            Builder.CopyTo(0, charsBuffers, 0, length);
            return length;
        }

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
