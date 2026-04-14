using System;
using StatsdClient.Statistic;

namespace StatsdClient.Bufferize
{
    /// <summary>
    /// SynchronousSender sends metrics synchronously on the calling thread.
    /// This is intended for serverless environments (e.g., AWS Lambda) where
    /// background threads may be frozen between invocations.
    /// </summary>
    internal class SynchronousSender : IStatsBufferize
    {
        [ThreadStatic]
        private static Stats _threadLocalStats;

        private readonly StatsRouter _statsRouter;
        private readonly object _lock = new object();

        public SynchronousSender(StatsRouter statsRouter)
        {
            _statsRouter = statsRouter ?? throw new ArgumentNullException(nameof(statsRouter));
        }

        public bool TryDequeueFromPool(out Stats stats)
        {
            if (_threadLocalStats == null)
            {
                _threadLocalStats = new Stats();
            }

            stats = _threadLocalStats;
            return true;
        }

        public void Send(Stats stats)
        {
            lock (_lock)
            {
                _statsRouter.Route(stats);
            }
        }

        public void Flush()
        {
            lock (_lock)
            {
                _statsRouter.Flush();
            }
        }

        public void Dispose()
        {
            Flush();
        }
    }
}
