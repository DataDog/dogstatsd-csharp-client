using System;
using System.Collections.Concurrent;
using System.Threading;

namespace StatsdClient.Utils
{
    internal class Pool<T>
        : IPool
        where T : AbstractPoolObject
    {
        private readonly Func<Pool<T>, T> _factory;
        private readonly ConcurrentQueue<T> _pool = new ConcurrentQueue<T>();
        private readonly int _maxAllocationCount;
        private int _allocationCount;

        public Pool(Func<Pool<T>, T> factory, int maxAllocationCount)
        {
            _factory = factory;
            _maxAllocationCount = maxAllocationCount;
            _allocationCount = 0;
        }

        public bool TryDequeue(out T result)
        {
            if (!_pool.TryDequeue(out result))
            {
                if (Interlocked.Increment(ref _allocationCount) > _maxAllocationCount)
                {
                    result = default(T);
                    Interlocked.Decrement(ref _allocationCount);
                    return false;
                }
                result = _factory(this);
            }

            return true;
        }

        public void Enqueue(object obj)
        {
            var v = obj as T;
            if (v == null)
            {
                throw new ArgumentException($"{obj} is not a valid argument");
            }

            v.Reset();
            _pool.Enqueue(v);
        }
    }
}