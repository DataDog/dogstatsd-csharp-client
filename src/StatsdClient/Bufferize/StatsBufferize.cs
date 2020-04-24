using System.Threading.Tasks;
using System;
using StatsdClient.Worker;

namespace StatsdClient.Bufferize
{
    /// <summary>
    /// StatsBufferize bufferizes metrics before sending them.
    /// </summary>
    class StatsBufferize : IStatsdUDP, IDisposable
    {
        readonly AsynchronousWorker<string> _worker;

        public StatsBufferize(
            BufferBuilder bufferBuilder,
            int workerMaxItemCount,
            TimeSpan? blockingQueueTimeout,
            TimeSpan maxIdleWaitBeforeSending)
        {
            var handler = new WorkerHandler(bufferBuilder, maxIdleWaitBeforeSending);

            // `handler` (and also `bufferBuilder`) do not need to be thread safe as long as workerMaxItemCount is 1.
            this._worker = new AsynchronousWorker<string>(handler,
                                                          new Waiter(),
                                                          1,
                                                          workerMaxItemCount,
                                                          blockingQueueTimeout);
        }

        public void Send(string command)
        {
            this._worker.TryEnqueue(command);
        }

        public void Dispose()
        {
            this._worker.Dispose();
        }

        public Task SendAsync(string command)
        {
            throw new NotSupportedException();
        }

        class WorkerHandler : IAsynchronousWorkerHandler<string>
        {
            readonly BufferBuilder _bufferBuilder;
            readonly TimeSpan _maxIdleWaitBeforeSending;
            System.Diagnostics.Stopwatch _stopwatch;

            public WorkerHandler(BufferBuilder bufferBuilder, TimeSpan maxIdleWaitBeforeSending)
            {
                _bufferBuilder = bufferBuilder;
                _maxIdleWaitBeforeSending = maxIdleWaitBeforeSending;
            }

            public void OnNewValue(string metric)
            {
                if (!_bufferBuilder.Add(metric))
                    throw new InvalidOperationException($"The metric size exceeds the buffer capacity: {metric}");
                _stopwatch = null;
            }

            public bool OnIdle()
            {
                if (_stopwatch == null)
                    _stopwatch = System.Diagnostics.Stopwatch.StartNew();

                if (_stopwatch.ElapsedMilliseconds > _maxIdleWaitBeforeSending.TotalMilliseconds)
                {
                    this._bufferBuilder.HandleBufferAndReset();

                    // No need to wait as sending the value takes time.
                    return false;
                }
                return true;
            }

            public void OnShutdown()
            {
                this._bufferBuilder.HandleBufferAndReset();
            }
        }
    }
}
