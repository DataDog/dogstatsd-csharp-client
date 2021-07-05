using System;
using System.Collections.Concurrent;
using System.Threading;

namespace StatsdClient.Worker
{
    /// <summary>
    /// ConcurrentQueueWithPool is a concurrent queue that also provides a pool of objects.
    /// The expected worflow is the following:
    /// Sender:
    ///     - Get an object from the pool with TryDequeueFromPool
    ///     - Initialize the object
    ///     - Enqueue the object in the queue with EnqueueValue
    /// Receiver:
    ///     - Get an object from the queue with TryDequeueValue
    ///     - Use the object
    ///     - When the object is not used, put the object in the pool with EnqueuePool.
    /// </summary>
    internal class ConcurrentQueueWithPool<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly ConcurrentQueue<T> _poolQueue = new ConcurrentQueue<T>();
        private readonly System.Diagnostics.Stopwatch stopWatch = System.Diagnostics.Stopwatch.StartNew();
        private readonly TimeSpan? _blockingQueueTimeout;
        private readonly Func<T> _factory;
        private volatile int _remainingAllocations;

        public ConcurrentQueueWithPool(Func<T> factory, int maxItemCount, TimeSpan? blockingQueueTimeout)
        {
            _blockingQueueTimeout = blockingQueueTimeout;
            _factory = factory;
            _remainingAllocations = maxItemCount;
        }

        public bool IsEmpty => _queue.IsEmpty;

        public bool TryDequeueFromPool(out T value)
        {
            var spinWait = default(SpinWait);
            var start = stopWatch.Elapsed;

            while (!_poolQueue.TryDequeue(out value))
            {
                if (_remainingAllocations > 0)
                {
                    if (Interlocked.Decrement(ref _remainingAllocations) >= 0)
                    {
                        value = _factory();
                        return true;
                    }
                }

                if (!_blockingQueueTimeout.HasValue || stopWatch.Elapsed.Subtract(start) > _blockingQueueTimeout.Value)
                {
                    value = default(T);
                    return false;
                }

                spinWait.SpinOnce();
            }

            return true;
        }

        public void EnqueuePool(T value) => _poolQueue.Enqueue(value);

        public void EnqueueValue(T value) => _queue.Enqueue(value);

        public bool TryDequeueValue(out T value) => _queue.TryDequeue(out value);
    }
}