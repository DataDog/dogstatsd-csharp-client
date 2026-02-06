using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Tests
{
    [TestFixture]
    public class AsyncDisposalTests
    {
        /// <summary>
        /// Manual stress / demo. Runs only when invoked explicitly.
        /// </summary>
        [Test]
        [Explicit("Manual benchmark for PR docs - demonstrates async vs sync under starvation")]
        [Timeout(120000)]
        public async Task CompareSyncVsAsync_UnderStarvation()
        {
            ThreadPool.GetMinThreads(out var origMin, out var origMinIO);
            ThreadPool.GetMaxThreads(out var origMax, out var origMaxIO);

            try
            {
                ThreadPool.SetMinThreads(2, 2);
                ThreadPool.SetMaxThreads(4, 4);

                long syncTime;
                long asyncTime;
                bool syncCompleted;
                bool asyncCompleted;

                // --- sync ---
                {
                    var dogstatsd = new DogStatsdService();
                    dogstatsd.Configure(new StatsdConfig
                    {
                        StatsdServerName = "127.0.0.1",
                        StatsdPort = 4321,
                    });

                    var releaseSignal = new ManualResetEventSlim(false);
                    var tasks = new List<Task>();

                    for (var i = 0; i < 10; i++)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            dogstatsd.Increment("test.metric");
                            releaseSignal.Wait(15000);
                        }));
                    }

                    Thread.Sleep(500);

                    var sw = Stopwatch.StartNew();
                    var disposeThread = new Thread(dogstatsd.Dispose);
                    disposeThread.Start();
                    syncCompleted = disposeThread.Join(10000);
                    sw.Stop();
                    syncTime = sw.ElapsedMilliseconds;

                    releaseSignal.Set();

                    if (!syncCompleted)
                    {
                        disposeThread.Join(5000);
                    }

                    Task.WaitAll(tasks.ToArray(), 5000);
                    releaseSignal.Dispose();
                }

                await Task.Delay(500);

                // --- async ---
                {
                    var dogstatsd = new DogStatsdService();
                    dogstatsd.Configure(new StatsdConfig
                    {
                        StatsdServerName = "127.0.0.1",
                        StatsdPort = 4321,
                    });

                    var releaseSignal = new ManualResetEventSlim(false);
                    var tasks = new List<Task>();

                    for (var i = 0; i < 10; i++)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            dogstatsd.Increment("test.metric");
                            releaseSignal.Wait(15000);
                        }));
                    }

                    await Task.Delay(500);

                    var sw = Stopwatch.StartNew();
                    var disposeTask = dogstatsd.DisposeAsync();
                    asyncCompleted = await Task.WhenAny(disposeTask, Task.Delay(10000)) == disposeTask;
                    sw.Stop();
                    asyncTime = sw.ElapsedMilliseconds;

                    releaseSignal.Set();
                    await Task.WhenAll(tasks);
                    releaseSignal.Dispose();
                }

                TestContext.WriteLine("=== THREAD POOL STARVATION BENCHMARK ===");
                TestContext.WriteLine("Thread pool: min=2, max=4");
                TestContext.WriteLine("Blocking tasks: 10");
                TestContext.WriteLine();
                TestContext.WriteLine($"SYNC  Dispose: {(syncCompleted ? $"{syncTime}ms" : $"blocked (>{syncTime}ms)")}");
                TestContext.WriteLine($"ASYNC Dispose: {(asyncCompleted ? $"{asyncTime}ms" : $"blocked (>{asyncTime}ms)")}");
            }
            finally
            {
                ThreadPool.SetMinThreads(origMin, origMinIO);
                ThreadPool.SetMaxThreads(origMax, origMaxIO);
            }
        }
    }
}
