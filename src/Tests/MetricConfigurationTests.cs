using System;
using System.Threading;
using NUnit.Framework;
using StatsdClient;
using Tests.Helpers;


namespace Tests
{
    [TestFixture]
    public class MetricConfigurationTest
    {
        private void testReceive(string testServerName, int testPort, string testCounterName, 
                                 string expectedOutput, string[] tags = null)
        {
            UdpListener udpListener = new UdpListener(testServerName, testPort);
            Thread listenThread = new Thread(new ParameterizedThreadStart(udpListener.Listen));
            listenThread.Start();
            Metrics.Increment(testCounterName, tags:tags);
            while(listenThread.IsAlive);
            Assert.AreEqual(expectedOutput, udpListener.GetAndClearLastMessages()[0]);
            udpListener.Dispose();
        }

        [Test]
        public void throw_exception_when_no_config_provided()
        {
            MetricsConfig metricsConfig = null;
            Assert.Throws<ArgumentNullException>(() => StatsdClient.Metrics.Configure(metricsConfig));
        }

        [Test]
        public void throw_exception_when_no_hostname_provided()
        {
            var metricsConfig = new MetricsConfig {};
            Assert.Throws<ArgumentNullException>(() => StatsdClient.Metrics.Configure(metricsConfig));
        }

        [Test]
        public void default_port_is_8125()
        {
            var metricsConfig = new MetricsConfig
            {
                StatsdServerName = "127.0.0.1"
            };
            StatsdClient.Metrics.Configure(metricsConfig);
            testReceive("127.0.0.1", 8125, "test", "test:1|c");
        }

        [Test]
        public void setting_port()
        {
            var metricsConfig = new MetricsConfig
            {
                StatsdServerName = "127.0.0.1",
                StatsdPort = 8126
            };
            StatsdClient.Metrics.Configure(metricsConfig);
            testReceive("127.0.0.1", 8126, "test", "test:1|c");
        }

        [Test]
        public void setting_prefix()
        {
            var metricsConfig = new MetricsConfig
            {
                StatsdServerName = "127.0.0.1",
                Prefix = "prefix"
            };
            StatsdClient.Metrics.Configure(metricsConfig);
            testReceive("127.0.0.1", 8125, "test", "prefix.test:1|c");
        }

        [Test]
        public void setting_globaltag()
        {
            var metricsConfig = new MetricsConfig
            {
                StatsdServerName = "127.0.0.1",
                Prefix = "prefix",
                Tags = new[] { "tag1" }
            };
            StatsdClient.Metrics.Configure(metricsConfig);
            testReceive("127.0.0.1", 8125, "test", "prefix.test:1|c|#tag1");
        }

        [Test]
        public void setting_localtagonly()
        {
            var metricsConfig = new MetricsConfig
            {
                StatsdServerName = "127.0.0.1",
                Prefix = "prefix"                
            };
            StatsdClient.Metrics.Configure(metricsConfig);
            testReceive("127.0.0.1", 8125, "test", "prefix.test:1|c|#tag1", new []{ "tag1"});
        }

        [Test]
        public void setting_local_andglobal_tag()
        {
            var metricsConfig = new MetricsConfig
            {
                StatsdServerName = "127.0.0.1",
                Prefix = "prefix",
                Tags = new[] { "tag1" }
            };
            StatsdClient.Metrics.Configure(metricsConfig);
            testReceive("127.0.0.1", 8125, "test", "prefix.test:1|c|#tag1,tag2", new []{ "tag2" });
        }

    }
}
