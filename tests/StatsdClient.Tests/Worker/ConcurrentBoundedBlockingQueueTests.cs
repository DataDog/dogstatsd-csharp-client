using System;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient.Worker;

namespace Tests
{
    [TestFixture]
    public class ConcurrentBoundedBlockingQueueTests
    {
        [Test]
        public void NoWaitWhenQueueIsNotFull()
        {
            var timeout = TimeSpan.FromSeconds(5);
            var queue = new ConcurrentBoundedBlockingQueue<int>(1, timeout);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Assert.True(queue.TryEnqueue(0));
            var task = Task.Run(() =>
            {
                Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
                Assert.True(queue.TryDequeue(out var v));
            });

            // This line should block for a short period, waiting task completion.
            Assert.True(queue.TryEnqueue(1));

            // Check we have no timeout.
            DurationTools.AssertLess(stopwatch.ElapsedMilliseconds, timeout);
            task.Wait();
        }

        [Test]
        public void WaitWhenQueueIsFull()
        {
            var timeout = TimeSpan.FromMilliseconds(100);
            var queue = new ConcurrentBoundedBlockingQueue<int>(1, timeout);

            Assert.True(queue.TryEnqueue(1));
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int expectedTimeoutCount = 3;
            for (int i = 0; i < expectedTimeoutCount; ++i)
                Assert.False(queue.TryEnqueue(1));

            // Timeout should occurs
            DurationTools.AssertGreater(
                stopwatch.ElapsedMilliseconds,
                timeout.Multiply(expectedTimeoutCount));
        }
    }
}