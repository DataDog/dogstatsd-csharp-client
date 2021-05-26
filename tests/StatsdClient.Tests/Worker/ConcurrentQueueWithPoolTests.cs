using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient.Worker;

namespace Tests
{
    [TestFixture]
    public class ConcurrentQueueWithPoolTests
    {
        [Test]
        public void TryDequeueFromPool()
        {
            var queue = new ConcurrentQueueWithPool<int>(2, null);

            Assert.True(queue.TryDequeueFromPool(out var _));
            Assert.True(queue.TryDequeueFromPool(out var _));
            Assert.False(queue.TryDequeueFromPool(out var _));

            queue.EnqueuePool(42);
            Assert.True(queue.TryDequeueFromPool(out var v));
            Assert.AreEqual(42, v);
            Assert.False(queue.TryDequeueFromPool(out var _));
        }

        [Test]
        public void TryDequeueFromPoolTimeout()
        {
            var queue = new ConcurrentQueueWithPool<int>(0, TimeSpan.FromSeconds(1));
            Assert.False(queue.TryDequeueFromPool(out var _));

            var task = Task.Run(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(1));
                queue.EnqueuePool(42);
                Thread.Sleep(TimeSpan.FromMilliseconds(1500));
                queue.EnqueuePool(43);
            });
            Assert.True(queue.TryDequeueFromPool(out var v));
            Assert.AreEqual(42, v);

            Assert.False(queue.TryDequeueFromPool(out var _));

            Assert.True(queue.TryDequeueFromPool(out v));
            Assert.AreEqual(43, v);
            task.Wait();
        }
    }
}