using System;
using NUnit.Framework;
using StatsdClient.Utils;
using Tests.Utils;

namespace Tests
{
    [TestFixture]
    public class PoolTests
    {
        [Test]
        public void TryDequeue()
        {
            var pool = new Pool<PoolObject>(p => new PoolObject(p), 1);

            Assert.True(pool.TryDequeue(out var v));
            Assert.False(pool.TryDequeue(out var _));

            v.Value = 1;
            v.Dispose();
            Assert.True(pool.TryDequeue(out v));
            Assert.AreEqual(0, v.Value);
        }

        [Test]
        public void AbstractPoolObjectFinalizer()
        {
            var pool = new Pool<PoolObject>(p => new PoolObject(p), 1);

            Assert.True(pool.TryDequeue(out var _));
            Assert.False(pool.TryDequeue(out var _));

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.True(pool.TryDequeue(out var _));
        }

        private class PoolObject : AbstractPoolObject
        {
            public PoolObject(IPool p)
            : base(p, Tools.ExceptionHandler)
            {
            }

            public int Value { get; set; }

            protected override void DoReset()
            {
                Value = 0;
            }
        }
    }
}