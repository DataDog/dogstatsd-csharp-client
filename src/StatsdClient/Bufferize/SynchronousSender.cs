using System;
using StatsdClient.Statistic;

namespace StatsdClient.Bufferize
{
    /// <summary>
    /// SynchronousSender sends metrics synchronously on the calling thread.
    /// This is intended for serverless environments (e.g., AWS Lambda) where
    /// background threads may be frozen between invocations.
    /// </summary>
    internal class SynchronousSender : IStatsSender
    {
        [ThreadStatic]
        private static Stats _threadLocalStats;

        private readonly StatsRouter _statsRouter;
        private readonly Action<Exception> _optionalExceptionHandler;
        private readonly object _lock = new object();

        public SynchronousSender(StatsRouter statsRouter, Action<Exception> optionalExceptionHandler = null)
        {
            _statsRouter = statsRouter ?? throw new ArgumentNullException(nameof(statsRouter));
            _optionalExceptionHandler = optionalExceptionHandler;
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
            try
            {
                lock (_lock)
                {
                    _statsRouter.Route(stats);
                }
            }
            catch (Exception e)
            {
                if (_optionalExceptionHandler != null)
                {
                    _optionalExceptionHandler.Invoke(e);
                }
                else
                {
                    throw;
                }
            }
        }

        public void Flush()
        {
            try
            {
                lock (_lock)
                {
                    _statsRouter.Flush();
                }
            }
            catch (Exception e)
            {
                if (_optionalExceptionHandler != null)
                {
                    _optionalExceptionHandler.Invoke(e);
                }
                else
                {
                    throw;
                }
            }
        }

        public void Dispose()
        {
            Flush();
        }
    }
}
