using System.Collections.Concurrent;
using System.Threading;

namespace StatsdClient.Worker
{
    /// <summary>
    /// ConcurrentBoundedQueue is a ConcurrentQueue with a bounded number of items.
    /// Note: Value is not enqueued when the queue is full.
    /// </summary>
    class ConcurrentBoundedQueue<T>
    {
        readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        // Queue size. It is much faster than calling ConcurrentQueue<T>.Count
        int _queueCurrentSize = 0;

        public ConcurrentBoundedQueue(int maxItemCount)
        {
            MaxItemCount = maxItemCount;
        }

        public virtual bool TryEnqueue(T value)
        {
            if (_queueCurrentSize >= MaxItemCount)
            {
                value = default(T);
                return false;
            }

            _queue.Enqueue(value);
            Interlocked.Increment(ref _queueCurrentSize);
            return true;
        }

        public virtual bool TryDequeue(out T value)
        {
            if (_queue.TryDequeue(out value))
            {
                Interlocked.Decrement(ref _queueCurrentSize);
                return true;
            }
            return false;
        }

        public int QueueCurrentSize { get { return _queueCurrentSize; } }
        public int MaxItemCount { get; }
    }
}