using System;
using NUnit.Framework;
using StatsdClient;
using Tests.StatsSender;
using Tests.Utils;

namespace Tests
{
    [TestFixture]
    public class DogStatsdServiceMetricsTests
    {
        static int MetricToSendCount = 100 * 1000;

        [Test]
        public void UDPBlockingQueue()
        {
            var config = new StatsdConfig
            {
                StatsdServerName = "127.0.0.1",
                StatsdPort = 8132
            };
            config.Advanced.MaxBlockDuration = TimeSpan.FromSeconds(3);
            config.Advanced.MaxMetricsInAsyncQueue = MetricToSendCount / 10;

            SendAndCheckMetricsAreReceived(config);
        }

#if !OS_WINDOWS
        [Test]
        public void UnixDomainSocketBlockingQueue()
        {
            using (var temporaryPath = new TemporaryPath())
            {
                var config = new StatsdConfig
                {
                    StatsdServerName = StatsdBuilder.UnixDomainSocketPrefix + temporaryPath.Path,
                };
                config.Advanced.MaxBlockDuration = TimeSpan.FromSeconds(3);
                config.Advanced.UDSBufferFullBlockDuration = TimeSpan.FromSeconds(3);
                config.Advanced.MaxMetricsInAsyncQueue = MetricToSendCount / 10;

                SendAndCheckMetricsAreReceived(config);
            }
        }
#endif

        static void SendAndCheckMetricsAreReceived(StatsdConfig config)
        {
            using (var service = new DogStatsdService())
            {
                using (var server = new SocketServer(config))
                {
                    service.Configure(config);
                    for (int i = 0; i < MetricToSendCount; ++i)
                        service.Increment($"test{i}", tags: new[] { "KEY:VALUE" });

                    var metricsReceived = server.Stop();
                    Assert.AreEqual(MetricToSendCount, metricsReceived.Count);
                    for (int i = 0; i < MetricToSendCount; ++i)
                        Assert.AreEqual($"test{i}:1|c|#KEY:VALUE", metricsReceived[i]);
                }
            }
        }
    }
}