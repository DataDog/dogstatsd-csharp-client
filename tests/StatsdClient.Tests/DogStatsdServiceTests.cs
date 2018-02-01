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
        private List<string> ReceiveData(IDogStatsd dogStatsdInstance, string testServerName, int testPort, Action sendData)
        {
            using (var udpListener = new UdpListener(testServerName, testPort))
            {
                var listenThread = new Thread(udpListener.Listen);
                listenThread.Start();

                sendData();

                while (listenThread.IsAlive) ;

                return udpListener.GetAndClearLastMessages();  
            }
        }

        

        //[Test]
        //public void throw_exception_when_no_config_provided()
        //{
        //    StatsdConfig metricsConfig = null;
        //    Assert.Throws<ArgumentNullException>(() => StatsdClient.DogStatsd.Configure(metricsConfig));
        //}

        //[Test]
        //public void throw_exception_when_no_hostname_provided()
        //{
        //    var metricsConfig = new StatsdConfig { };
        //    Assert.Throws<ArgumentNullException>(() => StatsdClient.DogStatsd.Configure(metricsConfig));
        //}

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
                var receivedData = ReceiveData(nonStaticServiceInstance, "127.0.0.1", 8125,
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
                var receivedData = ReceiveData(nonStaticServiceInstance, "127.0.0.1", 8126,
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
                var receivedData = ReceiveData(nonStaticServiceInstance, "127.0.0.1", 8125,
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
                var receivedData = ReceiveData(nonStaticServiceInstance, "127.0.0.1", 8129,
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
                var receivedData = ReceiveData(nonStaticServiceInstance, "127.0.0.1", 8130,
                    () => {
                        using (nonStaticServiceInstance.StartTimer("timer.test"))
                        {
                            Thread.Sleep(100);
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
                Assert.IsTrue((metricTimeInMs >= 100), "Processing should have taken at least 100ms");
                Assert.IsTrue((metricTimeInMs < 110), "Timer reported 10% higher than time taken in action");
            }  
        }
    }
}
