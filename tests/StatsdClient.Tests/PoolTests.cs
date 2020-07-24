using NUnit.Framework;
using StatsdClient.Utils;

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

        private class PoolObject : AbstractPoolObject
        {
            public PoolObject(IPool p)
            : base(p)
            {
            }

            public int Value { get; set; }

            public override void Reset()
            {
                Value = 0;
            }
        }
    }
}