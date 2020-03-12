using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Moq;
using System.Threading.Tasks;
using StatsdClient.Worker;
using System.Linq;

namespace Tests
{
    [TestFixture]
    public class AsynchronousWorkerTests
    {
        Mock<IAsynchronousWorkerHandler<int>> _handler;
        readonly List<AsynchronousWorker<int>> _workers = new List<AsynchronousWorker<int>>();

        [SetUp]
        public void Init()
        {
            _handler = new Mock<IAsynchronousWorkerHandler<int>>();
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var worker in _workers)
                worker.Dispose();
            _workers.Clear();
        }

        [Test]
        public void TryEnqueue()
        {
            var valueReceived = new ManualResetEvent(false);

            _handler.Setup(h => h.OnNewValue(42)).Callback(() => valueReceived.Set());
            var worker = CreateWorker();
            Assert.IsTrue(worker.TryEnqueue(42));
            Assert.IsTrue(valueReceived.WaitOne(TimeSpan.FromSeconds(3)));
        }

        [Test]
        public async Task OnIdle()
        {
            var idleDurations = new List<long>();
            var stopwatch = new System.Diagnostics.Stopwatch();
            _handler.Setup(h => h.OnIdle()).Returns(true).Callback(() =>
            {
                lock (stopwatch)
                {
                    if (!stopwatch.IsRunning)
                        stopwatch.Start();
                    else
                    {
                        idleDurations.Add(stopwatch.ElapsedMilliseconds);
                        stopwatch.Restart();
                    }
                }
            });

            var maxWaitDuration = TimeSpan.FromSeconds(1);
            var worker = CreateWorker(
                workerThreadCount: 1,
                minWaitDuration: TimeSpan.FromMilliseconds(200),
                maxWaitDuration: maxWaitDuration);

            // Wait to call OnIdle.
            await Task.Delay(maxWaitDuration.Multiply(3));
            worker.Dispose();

            // Check none duration is greater than maxWaitDuration
            DurationTools.AssertLess(idleDurations.Max(), maxWaitDuration);

            // Remove all durations closed to maxWaitDuration
            Assert.NotZero(idleDurations.RemoveAll(v => DurationTools.AreClose(v, maxWaitDuration)));

            // Drop the first value, as it is too big on the CI. 
            idleDurations.RemoveAt(0);

            Assert.That(idleDurations, Is.Ordered);
        }

        [Test]
        public void BlockingQueue()
        {
            var nonBlockingWorker = CreateWorker(maxItemCount: 0);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Assert.False(nonBlockingWorker.TryEnqueue(0));
            var nonBlockinDuration = stopwatch.ElapsedMilliseconds;

            var timeout = TimeSpan.FromMilliseconds(250);
            var blockingWorker = CreateWorker(maxItemCount: 0, nonBlockingQueueTimeout: timeout);
            stopwatch.Restart();
            Assert.False(blockingWorker.TryEnqueue(0));
            var blockingDuration = stopwatch.ElapsedMilliseconds;

            DurationTools.AssertClose(blockingDuration, timeout);
            DurationTools.AssertLess(nonBlockinDuration, TimeSpan.FromMilliseconds(blockingDuration));
        }

        [Test, Timeout(2000)]
        public void DisposeNotBlock()
        {
            var worker = CreateWorker();
            // Check we do not block
            worker.Dispose();
        }
        AsynchronousWorker<int> CreateWorker(
            int maxItemCount = 10,
            int workerThreadCount = 2,
            TimeSpan? nonBlockingQueueTimeout = null,
            TimeSpan? minWaitDuration = null,
            TimeSpan? maxWaitDuration = null)
        {
            var worker = new AsynchronousWorker<int>(
                _handler.Object,
                workerThreadCount,
                maxItemCount,
                nonBlockingQueueTimeout,
                minWaitDuration.HasValue ? minWaitDuration.Value : TimeSpan.FromMilliseconds(1),
                maxWaitDuration.HasValue ? maxWaitDuration.Value : TimeSpan.FromMilliseconds(100));
            _workers.Add(worker);
            return worker;
        }
    }
}