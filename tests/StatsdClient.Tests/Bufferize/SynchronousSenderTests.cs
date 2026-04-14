using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;
using StatsdClient.Bufferize;
using StatsdClient.Statistic;
using Tests.Utils;

namespace Tests
{
    [TestFixture]
    public class SynchronousSenderTests
    {
        [Test]
        public void TryDequeueFromPoolAlwaysSucceeds()
        {
            var handler = new BufferBuilderHandlerMock();
            var bufferBuilder = new BufferBuilder(handler, 1024, "\n", Tools.ExceptionHandler);
            var serializers = new Serializers
            {
                MetricSerializer = new MetricSerializer(new SerializerHelper(null, null), string.Empty),
            };
            var statsRouter = new StatsRouter(serializers, bufferBuilder, null);

            using (var sender = new SynchronousSender(statsRouter))
            {
                for (int i = 0; i < 100; i++)
                {
                    Assert.IsTrue(sender.TryDequeueFromPool(out var stats));
                    Assert.IsNotNull(stats);
                }
            }
        }

        [Test]
        public void SendAndFlushDeliversMetrics()
        {
            var handler = new BufferBuilderHandlerMock();
            var bufferBuilder = new BufferBuilder(handler, 1024, "\n", Tools.ExceptionHandler);
            var serializers = new Serializers
            {
                MetricSerializer = new MetricSerializer(new SerializerHelper(null, null), string.Empty),
            };
            var statsRouter = new StatsRouter(serializers, bufferBuilder, null);

            using (var sender = new SynchronousSender(statsRouter))
            {
                sender.TryDequeueFromPool(out var stats);
                stats.Kind = StatsKind.Metric;
                stats.Metric.MetricType = MetricType.Count;
                stats.Metric.StatName = "test.counter";
                stats.Metric.NumericValue = 42;
                stats.Metric.SampleRate = 1.0;
                stats.Metric.Tags = null;

                sender.Send(stats);

                // Buffer hasn't been flushed yet (not full)
                Assert.IsNull(handler.Buffer);

                sender.Flush();

                // After flush, data should be delivered
                Assert.IsNotNull(handler.Buffer);
                Assert.AreEqual("test.counter:42|c\n", handler.BufferToString());
            }
        }

        [Test]
        public void MultipleMetricsBatchInBuffer()
        {
            var handler = new BufferBuilderHandlerMock();
            var bufferBuilder = new BufferBuilder(handler, 1024, "\n", Tools.ExceptionHandler);
            var serializers = new Serializers
            {
                MetricSerializer = new MetricSerializer(new SerializerHelper(null, null), string.Empty),
            };
            var statsRouter = new StatsRouter(serializers, bufferBuilder, null);

            using (var sender = new SynchronousSender(statsRouter))
            {
                sender.TryDequeueFromPool(out var stats);

                stats.Kind = StatsKind.Metric;
                stats.Metric.MetricType = MetricType.Count;
                stats.Metric.StatName = "counter1";
                stats.Metric.NumericValue = 1;
                stats.Metric.SampleRate = 1.0;
                stats.Metric.Tags = null;
                sender.Send(stats);

                stats.Metric.StatName = "counter2";
                stats.Metric.NumericValue = 2;
                sender.Send(stats);

                // Nothing sent yet
                Assert.IsNull(handler.Buffer);

                sender.Flush();

                // Both metrics in a single packet
                Assert.IsNotNull(handler.Buffer);
                var result = handler.BufferToString();
                Assert.That(result, Does.Contain("counter1:1|c"));
                Assert.That(result, Does.Contain("counter2:2|c"));
            }
        }

        [Test]
        public void BufferAutoFlushesWhenFull()
        {
            var handler = new BufferBuilderHandlerMock();

            // Small buffer capacity: first metric fits, second triggers auto-flush of the first
            var bufferBuilder = new BufferBuilder(handler, 30, "\n", Tools.ExceptionHandler);
            var serializers = new Serializers
            {
                MetricSerializer = new MetricSerializer(new SerializerHelper(null, null), string.Empty),
            };
            var statsRouter = new StatsRouter(serializers, bufferBuilder, null);

            using (var sender = new SynchronousSender(statsRouter))
            {
                sender.TryDequeueFromPool(out var stats);
                stats.Kind = StatsKind.Metric;
                stats.Metric.MetricType = MetricType.Count;
                stats.Metric.StatName = "first.metric";
                stats.Metric.NumericValue = 1;
                stats.Metric.SampleRate = 1.0;
                stats.Metric.Tags = null;
                sender.Send(stats);

                // First metric fits in buffer, nothing flushed yet
                Assert.IsNull(handler.Buffer);

                // Second metric won't fit alongside the first, triggering auto-flush
                stats.Metric.StatName = "second.metric";
                stats.Metric.NumericValue = 2;
                sender.Send(stats);

                // First metric should have been auto-flushed
                Assert.IsNotNull(handler.Buffer);
                Assert.AreEqual("first.metric:1|c\n", handler.BufferToString());
            }
        }

        [Test]
        public void DisposeTriggersFlush()
        {
            var handler = new BufferBuilderHandlerMock();
            var bufferBuilder = new BufferBuilder(handler, 1024, "\n", Tools.ExceptionHandler);
            var serializers = new Serializers
            {
                MetricSerializer = new MetricSerializer(new SerializerHelper(null, null), string.Empty),
            };
            var statsRouter = new StatsRouter(serializers, bufferBuilder, null);

            var sender = new SynchronousSender(statsRouter);

            sender.TryDequeueFromPool(out var stats);
            stats.Kind = StatsKind.Metric;
            stats.Metric.MetricType = MetricType.Gauge;
            stats.Metric.StatName = "test.gauge";
            stats.Metric.NumericValue = 100;
            stats.Metric.SampleRate = 1.0;
            stats.Metric.Tags = null;
            sender.Send(stats);

            Assert.IsNull(handler.Buffer);

            sender.Dispose();

            Assert.IsNotNull(handler.Buffer);
            Assert.AreEqual("test.gauge:100|g\n", handler.BufferToString());
        }

        [Test]
        public void ThreadSafety()
        {
            var handler = new ConcurrentBufferBuilderHandler();
            var bufferBuilder = new BufferBuilder(handler, 1024, "\n", Tools.ExceptionHandler);
            var serializers = new Serializers
            {
                MetricSerializer = new MetricSerializer(new SerializerHelper(null, null), string.Empty),
            };
            var statsRouter = new StatsRouter(serializers, bufferBuilder, null);

            using (var sender = new SynchronousSender(statsRouter))
            {
                const int threadCount = 10;
                const int metricsPerThread = 100;
                var barrier = new Barrier(threadCount);
                var tasks = new Task[threadCount];

                for (int t = 0; t < threadCount; t++)
                {
                    int threadIndex = t;
                    tasks[t] = Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        for (int i = 0; i < metricsPerThread; i++)
                        {
                            sender.TryDequeueFromPool(out var stats);
                            stats.Kind = StatsKind.Metric;
                            stats.Metric.MetricType = MetricType.Count;
                            stats.Metric.StatName = $"thread{threadIndex}.counter";
                            stats.Metric.NumericValue = 1;
                            stats.Metric.SampleRate = 1.0;
                            stats.Metric.Tags = null;
                            sender.Send(stats);
                        }
                    });
                }

                Task.WaitAll(tasks);
                sender.Flush();

                // Verify no exceptions were thrown and all data was captured
                var allData = handler.GetAllData();
                Assert.IsNotEmpty(allData);

                // Verify each thread's metrics appear in the output
                for (int t = 0; t < threadCount; t++)
                {
                    Assert.That(allData, Does.Contain($"thread{t}.counter:1|c"));
                }
            }
        }

        [Test]
        public void FlushWhenEmptyDoesNotThrow()
        {
            var handler = new BufferBuilderHandlerMock();
            var bufferBuilder = new BufferBuilder(handler, 1024, "\n", Tools.ExceptionHandler);
            var serializers = new Serializers
            {
                MetricSerializer = new MetricSerializer(new SerializerHelper(null, null), string.Empty),
            };
            var statsRouter = new StatsRouter(serializers, bufferBuilder, null);

            using (var sender = new SynchronousSender(statsRouter))
            {
                Assert.DoesNotThrow(() => sender.Flush());
                Assert.DoesNotThrow(() => sender.Flush());
            }
        }

        [Test]
        public void ExceptionsForwardedToHandler()
        {
            var throwingHandler = new ThrowingBufferBuilderHandler();
            var bufferBuilder = new BufferBuilder(throwingHandler, 20, "\n", null);
            var serializers = new Serializers
            {
                MetricSerializer = new MetricSerializer(new SerializerHelper(null, null), string.Empty),
            };
            var statsRouter = new StatsRouter(serializers, bufferBuilder, null);

            Exception caughtException = null;
            Action<Exception> exceptionHandler = e => caughtException = e;

            using (var sender = new SynchronousSender(statsRouter, exceptionHandler))
            {
                sender.TryDequeueFromPool(out var stats);
                stats.Kind = StatsKind.Metric;
                stats.Metric.MetricType = MetricType.Count;
                stats.Metric.StatName = "will.fail";
                stats.Metric.NumericValue = 1;
                stats.Metric.SampleRate = 1.0;
                stats.Metric.Tags = null;

                // Send enough data to trigger a buffer flush (buffer is 20 bytes, metric is ~15)
                sender.Send(stats);
                sender.Send(stats);

                // Exception should have been caught by the handler, not propagated
                Assert.IsNotNull(caughtException);
                Assert.That(caughtException.Message, Does.Contain("Transport error"));
            }
        }

        /// <summary>
        /// Thread-safe handler that accumulates all buffers for verification.
        /// </summary>
        private class ConcurrentBufferBuilderHandler : IBufferBuilderHandler
        {
            private readonly ConcurrentBag<string> _buffers = new ConcurrentBag<string>();

            public void Handle(byte[] buffer, int length)
            {
                var data = new byte[length];
                Array.Copy(buffer, data, length);
                _buffers.Add(Encoding.UTF8.GetString(data));
            }

            public string GetAllData()
            {
                var sb = new StringBuilder();
                foreach (var buf in _buffers)
                {
                    sb.Append(buf);
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Handler that throws on Handle to simulate transport failures.
        /// </summary>
        private class ThrowingBufferBuilderHandler : IBufferBuilderHandler
        {
            public void Handle(byte[] buffer, int length)
            {
                throw new InvalidOperationException("Transport error");
            }
        }
    }
}
