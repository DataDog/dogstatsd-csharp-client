using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                new MetricSerializer(new SerializerHelper(null), string.Empty),
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