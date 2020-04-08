using System;
using NUnit.Framework;
using StatsdClient;
using System.Collections.Generic;
using Moq;
using System.Text;

namespace Tests
{
    [TestFixture]
    public class TelemetryTests
    {
        private Telemetry _telemetry;
        private readonly List<string> _metrics = new List<string>();

        [SetUp]
        public void Init()
        {
            var statsSender = new Mock<IStatsSender>();
            statsSender.Setup(s => s.Send(It.IsAny<byte[]>(), It.IsAny<int>()))
                .Callback<byte[], int>((bytes, l) => _metrics.Add(Encoding.UTF8.GetString(bytes, 0, l)));
            _telemetry = new Telemetry("1.0.0.0", TimeSpan.FromHours(1), statsSender.Object);
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
            AssertTelemetryReceived(new Dictionary<string, int> {
                                        { Telemetry.MetricsMetricName, 1 } });
        }

        [Test]
        public void ServiceCheckSent()
        {
            _telemetry.OnServiceCheckSent();
            AssertTelemetryReceived(new Dictionary<string, int> {
                                        { Telemetry.ServiceCheckMetricName, 1 } });
        }

        [Test]
        public void EventSent()
        {
            _telemetry.OnEventSent();
            AssertTelemetryReceived(new Dictionary<string, int> {
                                        { Telemetry.EventsMetricName, 1 } });
        }

        [Test]
        public void PacketSent()
        {
            _telemetry.OnPacketSent(42);
            AssertTelemetryReceived(new Dictionary<string, int> {
                                        { Telemetry.PacketsSentMetricName, 1 },
                                        { Telemetry.BytesSentMetricName, 42 } });
        }

        [Test]
        public void PacketDropped()
        {
            _telemetry.OnPacketDropped(42);
            AssertTelemetryReceived(new Dictionary<string, int> {
                                        { Telemetry.PacketsDroppedMetricName, 1 },
                                        { Telemetry.BytesDroppedMetricName, 42 } });
        }

        [Test]
        public void PacketsDroppedQueue()
        {
            _telemetry.OnPacketsDroppedQueue();
            AssertTelemetryReceived(new Dictionary<string, int> {
                                        { Telemetry.PacketsDroppedQueueMetricName, 1 } });
        }

        [Test]
        public void CheckTags()
        {
            _telemetry.OnMetricSent();
            _telemetry.Flush();
            Assert.AreEqual("datadog.dogstatsd.client.metrics:1|c|#" +
                "client:csharp,client_version:1.0.0.0,client_transport:uds", _metrics[0]);
        }

        private void AssertTelemetryReceived(Dictionary<string, int> expectedResults)
        {
            _telemetry.Flush();
            foreach (var m in _metrics)
            {
                var nameWithoutTags = m.Split("|")[0];
                var part = nameWithoutTags.Split(":");
                var metricName = part[0];
                var metricValue = Int32.Parse(part[1]);

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