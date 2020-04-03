using System;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;

namespace Tests
{
    [TestFixture]
    public class DogStatsdServiceTelemetryTests
    {
        private DogStatsdService _service;
        private StatsdConfig _config;

        [SetUp]
        public void Init()
        {
            _service = new DogStatsdService();
            _config = new StatsdConfig { StatsdServerName = "localhost" };
        }

        [TearDown]
        public void Cleanup()
        {
            _service.Dispose();
        }

        [Test]
        public void MetricsSent()
        {
            _service.Configure(_config);

            _service.Counter("test", 1);
            _service.Increment("test");
            _service.Decrement("test");

            Assert.AreEqual(3, _service.TelemetryCounters.MetricsSent);
        }

        [Test]
        public void EventsSent()
        {
            _service.Configure(_config);
            _service.Event("test", "test");
            Assert.AreEqual(1, _service.TelemetryCounters.EventsSent);
        }

        [Test]
        public void ServiceChecksSent()
        {
            _service.Configure(_config);
            _service.ServiceCheck("test", Status.OK);
            Assert.AreEqual(1, _service.TelemetryCounters.ServiceChecksSent);
        }

        [Test]
        public void PacketsDroppedQueue()
        {
            _config.Advanced.MaxMetricsInAsyncQueue = 0;
            _service.Configure(_config);
            _service.Event("test", "test");
            Assert.AreEqual(1, _service.TelemetryCounters.PacketsDroppedQueue);
        }

        [Test]
        public async Task PacketsSent()
        {
            _service.Configure(_config);
            _service.Increment("test");
            await Task.Delay(TimeSpan.FromMilliseconds(500));

            Assert.AreEqual(1, _service.TelemetryCounters.PacketsSent);
            Assert.AreEqual(8, _service.TelemetryCounters.BytesSent);
        }
    }
}