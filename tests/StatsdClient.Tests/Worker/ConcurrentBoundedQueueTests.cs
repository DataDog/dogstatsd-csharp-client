using NUnit.Framework;
using StatsdClient.Worker;

namespace Tests
{
    [TestFixture]
    public class ConcurrentBoundedQueueTests
    {
        [Test]
        public void TryEnqueueTryDequeue()
        {
            var queue = new ConcurrentBoundedQueue<int>(100);
            int value = 0;

            Assert.False(queue.TryDequeue(out value));
            Assert.True(queue.TryEnqueue(1));
            Assert.True(queue.TryEnqueue(2));

            Assert.True(queue.TryDequeue(out value));
            Assert.AreEqual(1, value);

            Assert.True(queue.TryDequeue(out value));
            Assert.AreEqual(2, value);

            Assert.False(queue.TryDequeue(out value));
        }

        [Test]
        public void TryEnqueueQueueFull()
        {
            var queue = new ConcurrentBoundedQueue<int>(3);

            Assert.True(queue.TryEnqueue(1));
            Assert.True(queue.TryEnqueue(2));
            Assert.True(queue.TryEnqueue(3));
            Assert.False(queue.TryEnqueue(4));

            int value = -1;
            Assert.True(queue.TryDequeue(out value));
            Assert.AreEqual(1, value);

            Assert.True(queue.TryEnqueue(5));
            Assert.False(queue.TryEnqueue(6));
        }
    }
}