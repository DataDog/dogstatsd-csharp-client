using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace StatsdClient.Worker
{
    /// <summary>
    /// AsynchronousWorker performs tasks asynchronously.
    /// `handler` must be thread safe if `workerThreadCount` > 1.
    /// </summary>
    internal class AsynchronousWorker<T> : IDisposable where T: new()
    {
        private static TimeSpan maxWaitDurationInFlush = TimeSpan.FromSeconds(3);
        private readonly ConcurrentQueue<int> _queue;
        private readonly T[] _buffer;
        private readonly ConcurrentQueue<int> _freeIndex;

        private readonly List<Task> _workers = new List<Task>();
        private readonly IAsynchronousWorkerHandler<T> _handler;
        private readonly IWaiter _waiter;
        private volatile bool _terminate = false;

        private volatile bool _requestFlush = false;
        private AutoResetEvent _flushEvent = new AutoResetEvent(false);

        public AsynchronousWorker(
            IAsynchronousWorkerHandler<T> handler,
            IWaiter waiter,
            int workerThreadCount,
            int maxItemCount,
            TimeSpan? blockingQueueTimeout)
        {
            _freeIndex = new ConcurrentQueue<int>();
            _buffer = new T[maxItemCount + 10];
            for (int i = 0; i < maxItemCount + 10; i++)
            {                
                _freeIndex.Enqueue(i);
            }
            // if (blockingQueueTimeout.HasValue)
            // {
            //     _queue = new ConcurrentBoundedBlockingQueue<int>(
            //         new ManualResetEventWrapper(),
            //         blockingQueueTimeout.Value,
            //         maxItemCount);
            // }
            // else
            // {
            //     _queue = new ConcurrentBoundedQueue<int>(maxItemCount);
            // }
            _queue = new ConcurrentQueue<int>();

            _handler = handler;
            _waiter = waiter;
            for (int i = 0; i < workerThreadCount; ++i)
            {
                _workers.Add(Task.Factory.StartNew(() => Dequeue(), TaskCreationOptions.LongRunning));
            }
        }

        public static TimeSpan MinWaitDuration { get; } = TimeSpan.FromMilliseconds(1);

        public static TimeSpan MaxWaitDuration { get; } = TimeSpan.FromMilliseconds(100);

        public int? GetFreeIndex()
        {
            if (_freeIndex.TryDequeue(out var v))
                return v;
            return null;
        }
        public ref T Get(int index)
        {
            return ref _buffer[index];
        }
        public bool TryEnqueue(int value)
        {
            _queue.Enqueue(value);
            return true;
        }

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
                    Debug.WriteLine(e.Message);
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
                    if (_queue.TryDequeue(out var v))
                    {                        
                        ref T v2 = ref _buffer[v];
                        _handler.OnNewValue(ref v2);
                        _freeIndex.Enqueue(v);
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
                    Debug.WriteLine(e.Message);
                    // This is the defined behavior of a ThreadAbortException, but it doesn't happen on
                    // the .NET Framework in 64-bit release builds using RyuJIT
                    throw;
                }
#endif
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }
    }
}