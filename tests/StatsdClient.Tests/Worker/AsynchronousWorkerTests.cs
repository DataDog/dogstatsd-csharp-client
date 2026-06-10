using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using StatsdClient.Worker;
using Tests.Utils;

namespace Tests
{
    [TestFixture]
    public class AsynchronousWorkerTests
    {
        private readonly List<AsynchronousWorker<int>> _workers = new List<AsynchronousWorker<int>>();
        private Mock<IAsynchronousWorkerHandler<int>> _handler;

        private Mock<IWaiter> _waiter;

        [SetUp]
        public void Init()
        {
            _handler = new Mock<IAsynchronousWorkerHandler<int>>();
            _handler.Setup(h => h.OnIdle());
            _waiter = new Mock<IWaiter>();
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var worker in _workers)
            {
                worker.Dispose();
            }

            _workers.Clear();
        }

        [Test]
        public void TryEnqueue()
        {
            var valueReceived = new ManualResetEvent(false);

            _handler.Setup(h => h.OnNewValue(42)).Callback(() => valueReceived.Set());
            var worker = CreateWorker();
            worker.Enqueue(42);
            Assert.IsTrue(valueReceived.WaitOne(TimeSpan.FromSeconds(3)));
        }

        [Test]
        [Timeout(30000)]
        public async Task OnIdle()
        {
            var waitDurationQueue = new ConcurrentQueue<TimeSpan>();

            _handler.Setup(h => h.OnIdle()).Returns(true);
            _waiter.Setup(w => w.Wait(It.IsAny<TimeSpan>()))
                   .Callback<TimeSpan>(t => waitDurationQueue.Enqueue(t));

            using (var worker = CreateWorker(workerThreadCount: 1))
            {
                while (waitDurationQueue.Count() < 100)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1));
                }
            }

            var waitDurations = new List<TimeSpan>(waitDurationQueue);

            Assert.GreaterOrEqual(waitDurations.Min(), AsynchronousWorker<int>.MinWaitDuration);
            Assert.LessOrEqual(waitDurations.Max(), AsynchronousWorker<int>.MaxWaitDuration);
            Assert.That(waitDurations, Is.Ordered);
        }

        [Test]
        [Timeout(2000)]
        public void DisposeNotBlock()
        {
            var worker = CreateWorker();
            // Check we do not block
            worker.Dispose();
        }

        [Test]
        public void Flush()
        {
            var worker = CreateWorker(workerThreadCount: 1);
            {
                worker.Flush();
                worker.Flush();
                _handler.Verify(h => h.Flush(), Times.Exactly(2));
            }
        }

        // Regression test for https://github.com/DataDog/dogstatsd-csharp-client/issues/204.
        // The worker must not inherit the caller's TaskScheduler; otherwise a long-running
        // Dequeue() loop can end up pinned to a UI SynchronizationContext and freeze the app.
        [Test]
        [Timeout(5000)]
        public void WorkerTasksDoNotInheritCallerScheduler()
        {
            const int workerThreadCount = 3;
            var trackingScheduler = new TrackingTaskScheduler();

            var outerTask = Task.Factory.StartNew(
                () => CreateWorker(workerThreadCount: workerThreadCount),
                CancellationToken.None,
                TaskCreationOptions.None,
                trackingScheduler);

            Assert.IsTrue(outerTask.Wait(TimeSpan.FromSeconds(3)));

            // The only task queued on the tracking scheduler should be the outer task itself.
            // If AsynchronousWorker inherits TaskScheduler.Current, the worker tasks would be
            // queued here too, bringing the count up to 1 + workerThreadCount.
            Assert.AreEqual(
                1,
                trackingScheduler.QueueCount,
                "AsynchronousWorker leaked worker tasks onto the caller's TaskScheduler (issue #204).");
        }

#if NETFRAMEWORK
        /// <summary>
        /// This test can only fail when run on the .NET Framework in 64-bit release build using RyuJIT.
        /// </summary>
        [Test]
        public void ThreadAbortExceptionExitsWorker()
        {
            var domain = AppDomain.CreateDomain("ThreadAbortExceptionExitsWorkerTest");

            var domainDelegate = (AppDomainDelegate)domain.CreateInstanceFromAndUnwrap(
                typeof(AppDomainDelegate).Assembly.Location,
                typeof(AppDomainDelegate).FullName);

            domainDelegate.Execute();

            Assert.DoesNotThrow(() => AppDomain.Unload(domain));
        }
#endif

        private AsynchronousWorker<int> CreateWorker(int workerThreadCount = 2)
        {
            var worker = new AsynchronousWorker<int>(
                () => 0,
                _handler.Object,
                _waiter.Object,
                workerThreadCount,
                10,
                null,
                Tools.ExceptionHandler);
            _workers.Add(worker);
            return worker;
        }

        private sealed class TrackingTaskScheduler : TaskScheduler
        {
            private int _queueCount;

            public int QueueCount => Volatile.Read(ref _queueCount);

            protected override void QueueTask(Task task)
            {
                Interlocked.Increment(ref _queueCount);
                ThreadPool.UnsafeQueueUserWorkItem(_ => TryExecuteTask(task), null);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;

            protected override IEnumerable<Task> GetScheduledTasks() => Array.Empty<Task>();
        }

#if NETFRAMEWORK
        private class AppDomainDelegate : MarshalByRefObject
        {
            public void Execute()
            {
                // This intentionally avoids referencing AsynchronousWorkerTests types
                // because the assembly would fail to load
                _ = new AsynchronousWorker<int>(
                    () => 0,
                    new Mock<IAsynchronousWorkerHandler<int>>().Object,
                    new Mock<IWaiter>().Object,
                    1,
                    10,
                    null,
                    Tools.ExceptionHandler);
            }
        }
#endif
    }
}