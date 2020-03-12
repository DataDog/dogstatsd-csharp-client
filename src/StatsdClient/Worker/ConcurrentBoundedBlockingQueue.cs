using System;
using System.Threading;

namespace StatsdClient.Worker
{
    /// <summary>
    /// ConcurrentBoundedBlockingQueue is the same as ConcurrentBoundedQueue but 
    /// it waits for `waitTimeout` before dropping the value when the queue is full.
    /// </summary>
    class ConcurrentBoundedBlockingQueue<T> : ConcurrentBoundedQueue<T>
    {
        readonly ManualResetEventSlim _queueIsFull = new ManualResetEventSlim(false);
        readonly TimeSpan _waitTimeout;

        public ConcurrentBoundedBlockingQueue(int maxItemCount, TimeSpan waitTimeout)
                : base(maxItemCount)
        {
            _waitTimeout = waitTimeout;
        }

        public override bool TryEnqueue(T value)
        {
            while (!base.TryEnqueue(value))
            {
                if (!_queueIsFull.Wait(_waitTimeout))
                    return false;
                _queueIsFull.Reset();
            }
            return true;
        }

        public override bool TryDequeue(out T value)
        {
            if (base.TryDequeue(out value))
            {
                _queueIsFull.Set();
                return true;
            }
            value = default(T);
            return false;
        }
    }
}
