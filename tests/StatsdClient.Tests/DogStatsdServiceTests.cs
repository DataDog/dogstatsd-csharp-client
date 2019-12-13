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
        private List<string> ReceiveData(string testServerName, int testPort, Action sendData)
        {
            using (var udpListener = new UdpListener(testServerName, testPort))
            {
                var listenThread = new Thread(udpListener.ListenAndWait);
                listenThread.Start();

                sendData();

                // Make sure we received the data
                System.Threading.Thread.Sleep(500);
                udpListener.Shutdown();
                listenThread.Join();
                
                return udpListener.GetAndClearLastMessages();  
            }
        }

        [Test]
        public void default_port_is_8125()
        {
            using (var nonStaticServiceInstance = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1"
                };

                nonStaticServiceInstance.Configure(metricsConfig);
                var receivedData = ReceiveData("127.0.0.1", 8125,
                    () => { nonStaticServiceInstance.Increment("test"); });

                Assert.AreEqual(new List<string> {"test:1|c"}, receivedData);
            }
        }

        [Test]
        public void setting_port()
        {
            using (var nonStaticServiceInstance = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8126
                };

                nonStaticServiceInstance.Configure(metricsConfig);
                var receivedData = ReceiveData("127.0.0.1", 8126,
                    () => { nonStaticServiceInstance.Increment("test"); });

                Assert.AreEqual(new List<string> { "test:1|c" }, receivedData);
            }            
        }


        [Test]
        public void setting_port_listen_on_other_port_should_return_no_data()
        {
            using (var nonStaticServiceInstance = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8126
                };
                nonStaticServiceInstance.Configure(metricsConfig);
                var receivedData = ReceiveData("127.0.0.1", 8125,
                    () => { nonStaticServiceInstance.Increment("test"); });

                Assert.AreEqual(0, receivedData.Count);
            }       
        }


        [Test]
        public void setting_prefix()
        {
            using (var nonStaticServiceInstance = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8129,
                    Prefix = "prefix"
                };
                nonStaticServiceInstance.Configure(metricsConfig);
                var receivedData = ReceiveData("127.0.0.1", 8129,
                    () => { nonStaticServiceInstance.Increment("test"); });

                Assert.AreEqual(new List<string> { "prefix.test:1|c" }, receivedData);
            }           
        }

        [Test]
        public void setting_prefix_starttimer()
        {
            using (var nonStaticServiceInstance = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                    StatsdPort = 8130,
                    Prefix = "prefix"
                };
                nonStaticServiceInstance.Configure(metricsConfig);
                var receivedData = ReceiveData("127.0.0.1", 8130,
                    () => {
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
                Assert.IsTrue((metricTimeInMs < 1100), "Timer reported 10% higher than time taken in action");
            }  
        }

        [Test]
        public void setting_with_env_arg()
        {
            Environment.SetEnvironmentVariable("DD_DOGSTATSD_PORT", "8131");
            Environment.SetEnvironmentVariable("DD_AGENT_HOST", "127.0.0.1");
            using (var nonStaticServiceInstance = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig{};

                nonStaticServiceInstance.Configure(metricsConfig);
                var receivedData = ReceiveData("127.0.0.1", 8131,
                    () => { nonStaticServiceInstance.Increment("test"); });

                Assert.AreEqual(new List<string> { "test:1|c" }, receivedData);
            }
            Environment.SetEnvironmentVariable("DD_DOGSTATSD_PORT", null);
            Environment.SetEnvironmentVariable("DD_AGENT_HOST", null);            
        }

        [Test]
        public void setting_entity_id_with_env_arg()
        {
            Environment.SetEnvironmentVariable("DD_ENTITY_ID", "foobar");
            using (var nonStaticServiceInstance = new DogStatsdService())
            {
                var metricsConfig = new StatsdConfig
                {
                    StatsdServerName = "127.0.0.1",
                     StatsdPort = 8132,
                };

                nonStaticServiceInstance.Configure(metricsConfig);
                var receivedData = ReceiveData("127.0.0.1", 8132,
                    () => { nonStaticServiceInstance.Increment("test"); });

                Assert.AreEqual(new List<string> { "test:1|c|#dd.internal.entity_id:foobar" }, receivedData);
            }
            Environment.SetEnvironmentVariable("DD_ENTITY_ID", null);
        }
    }
}
