using System;

namespace StatsdClient.Utils
{
    internal abstract class AbstractPoolObject : IDisposable
    {
        private readonly IPool _pool;
        private readonly Action<Exception> _optionalExceptionHandler;
        private bool _enqueue = false;

        public AbstractPoolObject(IPool pool, Action<Exception> optionalExceptionHandler)
        {
            _pool = pool;
            _optionalExceptionHandler = optionalExceptionHandler;
        }

        ~AbstractPoolObject()
        {
            try
            {
                Dispose(false);
            }
            catch (Exception e)
            {
                _optionalExceptionHandler?.Invoke(e);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Reset()
        {
            _enqueue = false;
            DoReset();
        }

        protected abstract void DoReset();

        protected void Dispose(bool disposing)
        {
            if (!_enqueue)
            {
                _pool.Enqueue(this);
            }

            _enqueue = true;
        }
    }
}