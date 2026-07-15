using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Moq;
using NUnit.Framework;
using StatsdClient;
using StatsdClient.Transport;
using Tests.Utils;

namespace Tests
{
    [TestFixture]
    public class TelemetryTests
    {
        private readonly List<string> _metrics = new List<string>();
        private Telemetry _telemetry;

        [SetUp]
        public void Init()
        {
            var transport = new Mock<ITransport>();
            transport.Setup(s => s.Send(It.IsAny<byte[]>(), It.IsAny<int>()))
                .Callback<byte[], int>((bytes, l) => _metrics.Add(Encoding.UTF8.GetString(bytes, 0, l)));
            transport.SetupGet(s => s.TelemetryClientTransport).Returns("uds");
            _telemetry = new Telemetry(
                new MetricSerializer(new SerializerHelper(null, null), string.Empty),
                "1.0.0.0",
                TimeSpan.FromHours(1),
                transport.Object,
                new string[] { "globalTagKey:globalTagValue" },
                Tools.ExceptionHandler);
        }

        [TearDown]
        public void Cleanup()
        {
            _telemetry.Dispose();
            _metrics.Clear();
        }

        [Test]
        public void MetricSent()
        {
            _telemetry.OnMetricSent();
            AssertTelemetryReceived(new Dictionary<string, int>
            {
                { Telemetry.MetricsMetricName, 1 },
            });
        }

        [Test]
        public void ServiceCheckSent()
        {
            _telemetry.OnServiceCheckSent();
            AssertTelemetryReceived(new Dictionary<string, int>
            {
                { Telemetry.ServiceCheckMetricName, 1 },
            });
        }

        [Test]
        public void EventSent()
        {
            _telemetry.OnEventSent();
            AssertTelemetryReceived(new Dictionary<string, int>
            {
                { Telemetry.EventsMetricName, 1 },
            });
        }

        [Test]
        public void PacketSent()
        {
            _telemetry.OnPacketSent(42);
            AssertTelemetryReceived(new Dictionary<string, int>
            {
                { Telemetry.PacketsSentMetricName, 1 },
                { Telemetry.BytesSentMetricName, 42 },
            });
        }

        [Test]
        public void PacketDropped()
        {
            _telemetry.OnPacketDropped(42);
            AssertTelemetryReceived(new Dictionary<string, int>
            {
                { Telemetry.PacketsDroppedMetricName, 1 },
                { Telemetry.BytesDroppedMetricName, 42 },
            });
        }

        [Test]
        public void PacketsDroppedQueue()
        {
            _telemetry.OnPacketsDroppedQueue();
            AssertTelemetryReceived(new Dictionary<string, int>
            {
                { Telemetry.PacketsDroppedQueueMetricName, 1 },
            });
        }

        [Test]
        public void AggregatedContextFlush()
        {
            _telemetry.OnAggregatedContextFlush(MetricType.Count, 10);
            _telemetry.OnAggregatedContextFlush(MetricType.Set, 20);
            _telemetry.OnAggregatedContextFlush(MetricType.Gauge, 30);
            _telemetry.Flush();
            var metrics = _metrics.Where(m => m.Contains(Telemetry.AggregatedContextByTypeName));
            var tags = "client:csharp,client_version:1.0.0.0,client_transport:uds,globalTagKey:globalTagValue,";
            var expected = new[]
            {
                $"{Telemetry.AggregatedContextByTypeName}:30|c|#{tags}metrics_type:gauge",
                $"{Telemetry.AggregatedContextByTypeName}:10|c|#{tags}metrics_type:count",
                $"{Telemetry.AggregatedContextByTypeName}:20|c|#{tags}metrics_type:set",
            };

            Assert.That(metrics, Is.EquivalentTo(expected));
        }

        [Test]
        public void CheckTags()
        {
            _telemetry.OnMetricSent();
            _telemetry.Flush();
            Assert.AreEqual(
                "datadog.dogstatsd.client.metrics:1|c|#" +
                "client:csharp,client_version:1.0.0.0,client_transport:uds," +
                "globalTagKey:globalTagValue", _metrics[0]);
        }

        [Test]
        public void FlushesDoNotOverlap()
        {
            var concurrencyLock = new object();
            int concurrentSends = 0;
            int maxConcurrentSends = 0;

            var transport = new Mock<ITransport>();
            transport.SetupGet(s => s.TelemetryClientTransport).Returns("uds");
            transport.Setup(s => s.Send(It.IsAny<byte[]>(), It.IsAny<int>()))
                .Callback<byte[], int>((bytes, l) =>
                {
                    lock (concurrencyLock)
                    {
                        concurrentSends++;
                        maxConcurrentSends = Math.Max(maxConcurrentSends, concurrentSends);
                    }

                    // Make a flush slow relative to the interval so overlapping callbacks would
                    // be observable if the timer were still periodic.
                    Thread.Sleep(20);

                    lock (concurrencyLock)
                    {
                        concurrentSends--;
                    }
                });

            using (new Telemetry(
                new MetricSerializer(new SerializerHelper(null, null), string.Empty),
                "1.0.0.0",
                TimeSpan.FromMilliseconds(50),
                transport.Object,
                Array.Empty<string>(),
                Tools.ExceptionHandler))
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            // A single flush sends sequentially on one thread, so concurrency stays 1 unless
            // two flushes ran at once.
            Assert.AreEqual(1, maxConcurrentSends);
        }

        [Test]
        public void DisposeDoesNotDeadlockDuringInFlightFlush()
        {
            Exception caughtException = null;
            int firstSend = 1;
            var flushStarted = new ManualResetEventSlim(false);
            var releaseFlush = new ManualResetEventSlim(false);

            var transport = new Mock<ITransport>();
            transport.SetupGet(s => s.TelemetryClientTransport).Returns("uds");
            transport.Setup(s => s.Send(It.IsAny<byte[]>(), It.IsAny<int>()))
                .Callback<byte[], int>((bytes, l) =>
                {
                    // Hold the first flush open so Dispose runs while a flush is in flight.
                    if (Interlocked.Exchange(ref firstSend, 0) == 1)
                    {
                        flushStarted.Set();
                        releaseFlush.Wait();
                    }
                });

            var telemetry = new Telemetry(
                new MetricSerializer(new SerializerHelper(null, null), string.Empty),
                "1.0.0.0",
                TimeSpan.FromMilliseconds(20),
                transport.Object,
                Array.Empty<string>(),
                e => caughtException = e);

            try
            {
                Assert.True(flushStarted.Wait(TimeSpan.FromSeconds(5)), "timer never dispatched a flush");

                // Dispose must complete without waiting for the in-flight flush: the flush holds no lock
                // while blocked in the transport, and the re-arm only takes _timerLock after the flush ends.
                var disposeThread = new Thread(() => telemetry.Dispose());
                disposeThread.Start();
                Assert.True(disposeThread.Join(TimeSpan.FromSeconds(5)), "Dispose deadlocked while a flush was in flight");
            }
            finally
            {
                // Always release the blocked flush callback so a failed assertion above does not
                // leave a thread-pool thread parked on releaseFlush for the rest of the run.
                releaseFlush.Set();
            }

            // Let the in-flight flush finish and attempt to re-arm after Dispose; it must not throw.
            Thread.Sleep(50);

            Assert.IsNull(caughtException);
        }

        private void AssertTelemetryReceived(Dictionary<string, int> expectedResults)
        {
            _telemetry.Flush();
            foreach (var m in _metrics)
            {
                var nameWithoutTags = m.Split('|')[0];
                var part = nameWithoutTags.Split(':');
                var metricName = part[0];
                var metricValue = int.Parse(part[1]);

                if (metricValue == 0)
                {
                    Assert.False(expectedResults.TryGetValue(metricName, out var res));
                }
                else
                {
                    Assert.AreEqual(metricValue, expectedResults[metricName]);
                }
            }
        }
    }
}
