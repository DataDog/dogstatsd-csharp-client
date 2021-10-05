using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Mono.Unix;
using NUnit.Framework;
using StatsdClient;
using Tests.Utils;

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
            _config.ClientSideAggregation = null;
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
            Assert.AreEqual(9, _service.TelemetryCounters.BytesSent);
        }

#if !OS_WINDOWS
        [Test]
        public async Task PacketsDropped()
        {
            using (var temporaryPath = new TemporaryPath())
            {
                using (var server = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.IP))
                {
                    var endPoint = new UnixEndPoint(temporaryPath.Path);
                    server.Bind(endPoint);

                    _config.StatsdServerName = StatsdBuilder.UnixDomainSocketPrefix + temporaryPath.Path;
                    _service.Configure(_config);

                    for (int i = 0; i < 10000; ++i)
                    {
                        _service.Increment("test");
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    Assert.Greater(_service.TelemetryCounters.PacketsDropped, 1);
                    Assert.Greater(_service.TelemetryCounters.BytesDropped, 8);
                    _service.Dispose();
                }
            }
        }
#endif
    }
}