using System;

namespace StatsdClient.Utils
{
    internal abstract class AbstractPoolObject : IDisposable
    {
        private readonly IPool _pool;
        private bool _disposed = false;

        public AbstractPoolObject(IPool pool)
        {
            _pool = pool;
        }

        ~AbstractPoolObject() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract void Reset();

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _pool.Enqueue(this);
            }

            _disposed = true;
        }
    }
}