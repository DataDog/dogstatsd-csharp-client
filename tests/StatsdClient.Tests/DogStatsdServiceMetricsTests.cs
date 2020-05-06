using System;
using NUnit.Framework;
using StatsdClient;
using Tests.Utils;

namespace Tests
{
    [TestFixture]
    public class DogStatsdServiceMetricsTests
    {
        [Test]
        public void UDPBlockingQueue()
        {
            // Send only 5 000 metrics because of UDP drops.
            var metricToSendCount = 5 * 1000;
            var config = new StatsdConfig
            {
                StatsdServerName = "127.0.0.1",
                StatsdPort = 8132,
            };
            config.Advanced.MaxBlockDuration = TimeSpan.FromSeconds(3);
            config.Advanced.MaxMetricsInAsyncQueue = metricToSendCount / 10;

            SendAndCheckMetricsAreReceived(config, metricToSendCount);
        }

#if !OS_WINDOWS
        [Test]
        public void UnixDomainSocketBlockingQueue()
        {
            var metricToSendCount = 100 * 1000;

            using (var temporaryPath = new TemporaryPath())
            {
                var config = new StatsdConfig
                {
                    StatsdServerName = StatsdBuilder.UnixDomainSocketPrefix + temporaryPath.Path,
                };
                config.Advanced.MaxBlockDuration = TimeSpan.FromSeconds(3);
                config.Advanced.UDSBufferFullBlockDuration = TimeSpan.FromSeconds(3);
                config.Advanced.MaxMetricsInAsyncQueue = metricToSendCount / 10;

                SendAndCheckMetricsAreReceived(config, metricToSendCount);
            }
        }
#endif

        private static void SendAndCheckMetricsAreReceived(StatsdConfig config, int metricToSendCount)
        {
            using (var service = new DogStatsdService())
            {
                using (var server = new SocketServer(config))
                {
                    service.Configure(config);
                    for (int i = 0; i < metricToSendCount; ++i)
                    {
                        service.Increment($"test{i}", tags: new[] { "KEY:VALUE" });
                    }

                    service.Dispose();
                    var metricsReceived = server.Stop();
                    Assert.AreEqual(metricToSendCount, metricsReceived.Count);
                    for (int i = 0; i < metricToSendCount; ++i)
                    {
                        Assert.AreEqual($"test{i}:1|c|#KEY:VALUE", metricsReceived[i]);
                    }
                }
            }
        }
    }
}