using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StatsdClient.Worker
{
    /// <summary>
    /// AsynchronousWorker performs tasks asynchronously.
    /// `handler` must be thread safe if `workerThreadCount` > 1.
    /// </summary>
    internal class AsynchronousWorker<T> : IDisposable
    {
        private static TimeSpan maxWaitDurationInFlush = TimeSpan.FromSeconds(3);
        private readonly List<Task> _workers = new List<Task>();
        private readonly IAsynchronousWorkerHandler<T> _handler;
        private readonly IWaiter _waiter;
        private readonly Action<Exception> _optionalExceptionHandler;
        private volatile bool _terminate = false;
        private volatile bool _requestFlush = false;
        private AutoResetEvent _flushEvent = new AutoResetEvent(false);
        private ConcurrentQueueWithPool<T> _queue;

        public AsynchronousWorker(
            Func<T> factory,
            IAsynchronousWorkerHandler<T> handler,
            IWaiter waiter,
            int workerThreadCount,
            int maxItemCount,
            TimeSpan? blockingQueueTimeout,
            Action<Exception> optionalExceptionHandler)
        {
            _queue = new ConcurrentQueueWithPool<T>(factory, maxItemCount, blockingQueueTimeout);
            _handler = handler;
            _waiter = waiter;
            _optionalExceptionHandler = optionalExceptionHandler;
            for (int i = 0; i < workerThreadCount; ++i)
            {
                _workers.Add(Task.Factory.StartNew(() => Dequeue(), TaskCreationOptions.LongRunning));
            }
        }

        public static TimeSpan MinWaitDuration { get; } = TimeSpan.FromMilliseconds(1);

        public static TimeSpan MaxWaitDuration { get; } = TimeSpan.FromMilliseconds(100);

        public void Enqueue(T value) => _queue.EnqueueValue(value);

        public bool TryDequeueFromPool(out T value) => _queue.TryDequeueFromPool(out value);

        public void Flush()
        {
            var remainingWaitCount = maxWaitDurationInFlush.TotalMilliseconds / MinWaitDuration.TotalMilliseconds;
            while (!_queue.IsEmpty && remainingWaitCount > 0)
            {
                _waiter.Wait(MinWaitDuration);
                --remainingWaitCount;
            }

            _requestFlush = true;
            _flushEvent.WaitOne(maxWaitDurationInFlush);
        }

        public void Dispose()
        {
            if (!_terminate)
            {
                Flush();
                _terminate = true;
                try
                {
                    foreach (var worker in _workers)
                    {
                        worker.Wait();
                    }

                    _flushEvent.Dispose();
                }
                catch (Exception e)
                {
                    _optionalExceptionHandler?.Invoke(e);
                }

                _workers.Clear();
            }
        }

        private void Dequeue()
        {
            var waitDuration = MinWaitDuration;

            while (true)
            {
                try
                {
                    if (_queue.TryDequeueValue(out var v))
                    {
                        try
                        {
                            _handler.OnNewValue(v);
                        }
                        finally
                        {
                            _queue.EnqueuePool(v);
                        }

                        waitDuration = MinWaitDuration;
                    }
                    else
                    {
                        if (_requestFlush)
                        {
                            _handler.Flush();
                            _requestFlush = false;
                            _flushEvent.Set();
                        }

                        if (_terminate)
                        {
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
#if NETFRAMEWORK
                catch (ThreadAbortException e)
                {
                    _optionalExceptionHandler?.Invoke(e);
                    // This is the defined behavior of a ThreadAbortException, but it doesn't happen on
                    // the .NET Framework in 64-bit release builds using RyuJIT
                    throw;
                }
#endif
                catch (Exception e)
                {
                    _optionalExceptionHandler?.Invoke(e);
                }
            }
        }
    }
}