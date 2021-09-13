using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;
using Tests.Helpers;

namespace Tests
{
    [TestFixture]
    public class StatsdConfigurationTests
    {
        [Test]
        public void Throw_exception_when_no_config_provided()
        {
            var sut = CreateSut();
            StatsdConfig metricsConfig = null;
            Assert.Throws<ArgumentNullException>(() => sut.Configure(metricsConfig));
        }

        [Test]
        public void Throw_exception_when_no_hostname_provided()
        {
            var sut = CreateSut();
            var metricsConfig = new StatsdConfig { };
            Assert.Throws<ArgumentNullException>(() => sut.Configure(metricsConfig));
        }

        [Test]
        public void Default_port_is_8125()
        {
            using (var sut = CreateSut())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                };
                sut.Configure(metricsConfig);
                TestReceive("127.0.0.1", 8125, "test", "test:1|c\n", sut);
            }
        }

        [Test]
        public void Setting_port()
        {
            using (var sut = CreateSut())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8126,
                };
                sut.Configure(metricsConfig);
                TestReceive("127.0.0.1", 8126, "test", "test:1|c\n", sut);
            }
        }

        [Test]
        public void Setting_prefix()
        {
            using (var sut = CreateSut())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8129,
                    Prefix = "prefix",
                };
                sut.Configure(metricsConfig);
                TestReceive("127.0.0.1", 8129, "test", "prefix.test:1|c\n", sut);
            }
        }

        [Test]
        public void Service_cannot_be_configured_more_than_once()
        {
            using (var sut = CreateSut())
            {
                StatsdConfig metricsConfig = new StatsdConfig()
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8129,
                    Prefix = "prefix",
                };

                sut.Configure(metricsConfig);

                Assert.Throws<InvalidOperationException>(() => sut.Configure(metricsConfig));
            }
        }

        private DogStatsdService CreateSut()
        {
            return new DogStatsdService();
        }

        private void TestReceive(
            string testServerName,
            int testPort,
            string testCounterName,
            string expectedOutput,
            DogStatsdService dogStatsdService)
        {
            using (UdpListener udpListener = new UdpListener(testServerName, testPort))
            {
                Thread listenThread = new Thread(udpListener.ListenAndWait);
                listenThread.Start();

                dogStatsdService.Increment(testCounterName);

                dogStatsdService.Dispose();
                udpListener.Shutdown();
                listenThread.Join();

                Assert.AreEqual(expectedOutput, udpListener.GetAndClearLastMessages()[0]);
            }
        }
    }
}
