using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StatsdClient.Worker
{
    /// <summary>
    /// AsynchronousWorker performs tasks asynchronously.
    /// `handler` must be thread safe if `workerThreadCount` > 1.
    /// </summary>    
    class AsynchronousWorker<T> : IDisposable
    {
        readonly ConcurrentBoundedQueue<T> _queue;
        readonly List<Task> _workers = new List<Task>();
        readonly IAsynchronousWorkerHandler<T> _handler;
        readonly TimeSpan _minWaitDuration;
        readonly TimeSpan _maxWaitDuration;
        volatile bool _terminate = false;

        public AsynchronousWorker(
            IAsynchronousWorkerHandler<T> handler,
            int workerThreadCount,
            int maxItemCount,
            TimeSpan? blockingQueueTimeout)
            : this(
                handler,
                workerThreadCount,
                maxItemCount,
                blockingQueueTimeout,
                TimeSpan.FromMilliseconds(1),
                TimeSpan.FromMilliseconds(100))
        {
        }

        public AsynchronousWorker(
            IAsynchronousWorkerHandler<T> handler,
            int workerThreadCount,
            int maxItemCount,
            TimeSpan? blockingQueueTimeout,
            TimeSpan minWaitDuration,
            TimeSpan maxWaitDuration)
        {
            if (blockingQueueTimeout.HasValue)
                _queue = new ConcurrentBoundedBlockingQueue<T>(maxItemCount, blockingQueueTimeout.Value);
            else
                _queue = new ConcurrentBoundedQueue<T>(maxItemCount);

            _handler = handler;
            _minWaitDuration = minWaitDuration;
            _maxWaitDuration = maxWaitDuration;
            for (int i = 0; i < workerThreadCount; ++i)
                _workers.Add(Task.Run(() => Dequeue()));
        }

        public bool TryEnqueue(T value)
        {
            return _queue.TryEnqueue(value);
        }

        void Dequeue()
        {
            var waitDuration = _minWaitDuration;

            while (true)
            {
                if (_queue.TryDequeue(out var v))
                {
                    _handler.OnNewValue(v);
                    waitDuration = _minWaitDuration;
                }
                else
                {
                    if (_terminate)
                        return;

                    if (_handler.OnIdle())
                    {
                        Task.Delay(waitDuration).Wait();
                        waitDuration = waitDuration + waitDuration;
                        if (waitDuration > _maxWaitDuration)
                            waitDuration = _maxWaitDuration;
                    }
                }
            }
        }

        public void Dispose()
        {
            _terminate = true;
            foreach (var worker in _workers)
                worker.Wait();
            _workers.Clear();
        }
    }
}