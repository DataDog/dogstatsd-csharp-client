using System;

namespace StatsdClient.Utils
{
    internal abstract class AbstractPoolObject : IDisposable
    {
        private readonly IPool _pool;

        public AbstractPoolObject(IPool pool)
        {
            _pool = pool;
        }

        public void Dispose()
        {
            _pool.Enqueue(this);
        }

        public abstract void Reset();
    }
}