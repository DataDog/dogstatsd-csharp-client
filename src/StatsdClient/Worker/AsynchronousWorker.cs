using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StatsdClient.Worker
{
    /// <summary>
    /// AsynchronousWorker performs tasks asynchronously.
    /// `handler` must be thread safe if `workerThreadCount` > 1.
    /// </summary>
    internal class AsynchronousWorker<T> : IDisposable
    {
        public static TimeSpan MinWaitDuration { get; } = TimeSpan.FromMilliseconds(1);

        public static TimeSpan MaxWaitDuration { get; } = TimeSpan.FromMilliseconds(100);

        private static TimeSpan maxWaitDurationInDispose = TimeSpan.FromSeconds(3);

        private readonly ConcurrentBoundedQueue<T> _queue;
        private readonly List<Task> _workers = new List<Task>();
        private readonly IAsynchronousWorkerHandler<T> _handler;
        private readonly IWaiter _waiter;
        private volatile bool _terminate = false;

        public AsynchronousWorker(
            IAsynchronousWorkerHandler<T> handler,
            IWaiter waiter,
            int workerThreadCount,
            int maxItemCount,
            TimeSpan? blockingQueueTimeout)
        {
            if (blockingQueueTimeout.HasValue)
            {
                _queue = new ConcurrentBoundedBlockingQueue<T>(
                    new ManualResetEventWrapper(),
                    blockingQueueTimeout.Value,
                    maxItemCount);
            }
            else
            {
                _queue = new ConcurrentBoundedQueue<T>(maxItemCount);
            }

            _handler = handler;
            _waiter = waiter;
            for (int i = 0; i < workerThreadCount; ++i)
            {
                _workers.Add(Task.Run(() => Dequeue()));
            }
        }

        public bool TryEnqueue(T value)
        {
            return _queue.TryEnqueue(value);
        }

        private void Dequeue()
        {
            var waitDuration = MinWaitDuration;

            while (true)
            {
                if (_queue.TryDequeue(out var v))
                {
                    _handler.OnNewValue(v);
                    waitDuration = MinWaitDuration;
                }
                else
                {
                    if (_terminate)
                    {
                        _handler.OnShutdown();
                        return;
                    }

                    if (_handler.OnIdle())
                    {
                        _waiter.Wait(waitDuration);
                        waitDuration = waitDuration + waitDuration;
                        if (waitDuration > MaxWaitDuration)
                        {
                            waitDuration = MaxWaitDuration;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            var remainingWaitCount = maxWaitDurationInDispose.TotalMilliseconds / MinWaitDuration.TotalMilliseconds;
            while (_queue.QueueCurrentSize > 0 && remainingWaitCount > 0)
            {
                _waiter.Wait(MinWaitDuration);
                --remainingWaitCount;
            }

            _terminate = true;
            foreach (var worker in _workers)
            {
                worker.Wait();
            }

            _workers.Clear();
        }
    }
}