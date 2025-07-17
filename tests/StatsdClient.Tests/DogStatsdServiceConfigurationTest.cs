using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using StatsdClient;
using Tests.Helpers;

namespace Tests
{
    [TestFixture]
    public class DogStatsdServiceConfigurationTest
    {
        [Test]
        public void Setting_port()
        {
            using (var nonStaticServiceInstance = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8127,
                    OriginDetection = false,
                };

                nonStaticServiceInstance.Configure(metricsConfig);
                var receivedData = ReceiveData(
                    nonStaticServiceInstance,
                    "127.0.0.1",
                    8127,
                    () => { nonStaticServiceInstance.Increment("test"); });

                Assert.AreEqual(new List<string> { "test:1|c\n" }, receivedData);
            }
        }

        [Test]
        public void Setting_port_listen_on_other_port_should_return_no_data()
        {
            using (var nonStaticServiceInstance = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 9128,
                    OriginDetection = false,
                };
                nonStaticServiceInstance.Configure(metricsConfig);
                var receivedData = ReceiveData(
                    nonStaticServiceInstance,
                    "127.0.0.1",
                    8128,
                    () => { nonStaticServiceInstance.Increment("test"); });

                Assert.AreEqual(0, receivedData.Count);
            }
        }

        [Test]
        public void Setting_prefix()
        {
            using (var nonStaticServiceInstance = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8129,
                    Prefix = "prefix",
                    OriginDetection = false,
                };
                nonStaticServiceInstance.Configure(metricsConfig);
                var receivedData = ReceiveData(
                    nonStaticServiceInstance,
                    "127.0.0.1",
                    8129,
                    () => { nonStaticServiceInstance.Increment("test"); });

                Assert.AreEqual(new List<string> { "prefix.test:1|c\n" }, receivedData);
            }
        }

        [Test]
        public void Setting_prefix_starttimer()
        {
            using (var nonStaticServiceInstance = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8130,
                    Prefix = "prefix",
                    OriginDetection = false,
                };
                nonStaticServiceInstance.Configure(metricsConfig);
                var receivedData = ReceiveData(
                    nonStaticServiceInstance,
                    "127.0.0.1",
                    8130,
                    () =>
                    {
                        using (nonStaticServiceInstance.StartTimer("timer.test"))
                        {
                            Thread.Sleep(1000);
                        }
                    });

                Assert.AreEqual(1, receivedData.Count);

                var metricResultSplit = receivedData[0].Split(':');

                Assert.AreEqual(2, metricResultSplit.Length);

                var metricNameWithPrefix = metricResultSplit[0];
                var metricTimeAndType = metricResultSplit[1];

                Assert.AreEqual("prefix.timer.test", metricNameWithPrefix);

                var metricTimeInMsSplit = metricTimeAndType.Split('|');
                Assert.AreEqual(2, metricTimeInMsSplit.Length);

                var metricTimeInMs = Convert.ToInt32(metricTimeInMsSplit[0]);
                Assert.IsTrue((metricTimeInMs >= 1000), "Processing should have taken at least 1000ms");
                Assert.IsTrue((metricTimeInMs < 1300), $"Timer reported 30% higher than time taken in action: {metricTimeInMs} VS 1300");
            }
        }

        [Test]
        public void Setting_with_env_arg()
        {
            Environment.SetEnvironmentVariable("DD_DOGSTATSD_PORT", "8131");
            Environment.SetEnvironmentVariable("DD_AGENT_HOST", "127.0.0.1");
            Environment.SetEnvironmentVariable("DD_ORIGIN_DETECTION_ENABLED", "false");
            using (var nonStaticServiceInstance = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig { };

                nonStaticServiceInstance.Configure(metricsConfig);
                var receivedData = ReceiveData(
                    nonStaticServiceInstance,
                    "127.0.0.1",
                    8131,
                    () => { nonStaticServiceInstance.Increment("test"); });

                Assert.AreEqual(new List<string> { "test:1|c\n" }, receivedData);
            }

            Environment.SetEnvironmentVariable("DD_DOGSTATSD_PORT", null);
            Environment.SetEnvironmentVariable("DD_AGENT_HOST", null);
        }

        [TestCase("DD_ENTITY_ID", "dd.internal.entity_id")]
        [TestCase(StatsdConfig.ServiceEnvVar, StatsdConfig.ServiceTagKey)]
        [TestCase(StatsdConfig.VersionEnvVar, StatsdConfig.VersionTagKey)]
        [TestCase(StatsdConfig.EnvironmentEnvVar, StatsdConfig.EnvironmentTagKey)]
        public void Setting_tag_with_env_arg(string envVar, string tag)
        {
            Environment.SetEnvironmentVariable(envVar, "foobar");

            try
            {
                using (var nonStaticServiceInstance = new DogStatsdService())
                {
                    var metricsConfig = new StatsdConfig
                    {
                        StatsdServerName = "127.0.0.1",
                        StatsdPort = 8132,
                        OriginDetection = false,
                    };

                    nonStaticServiceInstance.Configure(metricsConfig);
                    var receivedData = ReceiveData(
                        nonStaticServiceInstance,
                        "127.0.0.1",
                        8132,
                        () => { nonStaticServiceInstance.Increment("test"); });

                    Assert.AreEqual(new List<string> { $"test:1|c|#{tag}:foobar\n" }, receivedData);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(envVar, null);
            }
        }

        [Test]
        [Timeout(5000)]
        public void Test_message_too_long()
        {
            using (var service = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8133,
                    StatsdMaxUDPPacketSize = 10,
                    OriginDetection = false,
                };
                service.Configure(metricsConfig);

                var receivedData = ReceiveData(
                    service,
                    metricsConfig.StatsdServerName,
                    8133,
                    () =>
                {
                    service.Increment("test");
                    service.Increment("too_long_message_which_will_be_dropped");
                });
                Assert.AreEqual(new List<string> { "test:1|c\n" }, receivedData);
            }
        }

        private List<string> ReceiveData(DogStatsdService dogstasdService, string testServerName, int testPort, Action sendData)
        {
            using (var udpListener = new UdpListener(testServerName, testPort))
            {
                var listenThread = new Thread(udpListener.ListenAndWait);
                listenThread.Start();

                sendData();

                dogstasdService.Dispose();
                udpListener.Shutdown();
                listenThread.Join();

                return udpListener.GetAndClearLastMessages();
            }
        }
    }
}
