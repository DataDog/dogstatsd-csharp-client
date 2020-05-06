using System;
using Moq;
using NUnit.Framework;
using StatsdClient.Worker;

namespace Tests
{
    [TestFixture]
    public class ConcurrentBoundedBlockingQueueTests
    {
        private Mock<IManualResetEvent> _mock;
        private ConcurrentBoundedBlockingQueue<int> _queue;

        [SetUp]
        public void Init()
        {
            _mock = new Mock<IManualResetEvent>();
            _queue = new ConcurrentBoundedBlockingQueue<int>(
                _mock.Object,
                TimeSpan.FromSeconds(5),
                maxItemCount: 1);
        }

        [Test]
        public void NoWaitWhenQueueIsNotFull()
        {
            Assert.AreEqual(1, _queue.MaxItemCount);

            Assert.True(_queue.TryEnqueue(0));
            _mock.Verify(m => m.Reset(), Times.Once);

            Assert.True(_queue.TryDequeue(out var v));
            _mock.Verify(m => m.Set(), Times.Once);

            Assert.True(_queue.TryEnqueue(1));
            // Check TryEnqueue not block
            _mock.Verify(m => m.Wait(It.IsAny<TimeSpan>()), Times.Never);
        }

        [Test]
        public void WaitWhenQueueIsFull()
        {
            Assert.AreEqual(1, _queue.MaxItemCount);

            Assert.True(_queue.TryEnqueue(1));
            for (int i = 0; i < 3; ++i)
            {
                Assert.False(_queue.TryEnqueue(1));
            }

            _mock.Verify(m => m.Wait(It.IsAny<TimeSpan>()), Times.Exactly(3));
        }
    }
}