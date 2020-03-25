using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;
using Tests.Helpers;


namespace Tests
{
    [TestFixture]
    public class StatsdConfigurationTest
    {
        private void testReceive(string testServerName, int testPort, string testCounterName,
            string expectedOutput, DogStatsdService dogStatsdService)
        {
            UdpListener udpListener = new UdpListener(testServerName, testPort);
            Thread listenThread = new Thread(new ParameterizedThreadStart(udpListener.Listen));
            listenThread.Start();
            dogStatsdService.Increment(testCounterName);
            while (listenThread.IsAlive) ;
            Assert.AreEqual(expectedOutput, udpListener.GetAndClearLastMessages()[0]);
            udpListener.Dispose();
        }

        private DogStatsdService CreateSut()
        {
            return new DogStatsdService();
        }

        [Test]
        public void throw_exception_when_no_config_provided()
        {
            var sut = CreateSut();
            StatsdConfig metricsConfig = null;
            Assert.Throws<ArgumentNullException>(() => sut.Configure(metricsConfig));
        }

        [Test]
        public void throw_exception_when_no_hostname_provided()
        {
            var sut = CreateSut();
            var metricsConfig = new StatsdConfig { };
            Assert.Throws<ArgumentNullException>(() => sut.Configure(metricsConfig));
        }

        [Test]
        public void default_port_is_8125()
        {
            using (var sut = CreateSut())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1"
                };
                sut.Configure(metricsConfig);
                testReceive("127.0.0.1", 8125, "test", "test:1|c", sut);
            }
        }

        [Test]
        public void setting_port()
        {
            using (var sut = CreateSut())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8126
                };
                sut.Configure(metricsConfig);
                testReceive("127.0.0.1", 8126, "test", "test:1|c", sut);
            }
        }

        [Test]
        public void setting_prefix()
        {
            using (var sut = CreateSut())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8129,
                    Prefix = "prefix"
                };
                sut.Configure(metricsConfig);
                testReceive("127.0.0.1", 8129, "test", "prefix.test:1|c", sut);
            }
        }

        [Test]
        public void service_cannot_be_configured_more_than_once()
        {
            using (var sut = CreateSut())
            {
                StatsdConfig metricsConfig = new StatsdConfig()
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8129,
                    Prefix = "prefix"
                };

                sut.Configure(metricsConfig);

                Assert.Throws<InvalidOperationException>(() => sut.Configure(metricsConfig));
            }
        }
    }
}
