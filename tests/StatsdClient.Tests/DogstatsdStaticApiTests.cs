using System;
using System.Threading;
using NUnit.Framework;
using StatsdClient;
using Tests.Helpers;

namespace Tests
{
    /// <summary>
    /// This test fixture targets the static API for Dogstatsd and as such it must be run in the same
    /// manner it would be in a client application.
    /// That is, the configuration must only be called once and cannot be called again since in an application
    /// this would almost certainly be an error.
    /// For this fixture to work properly, it must be run single threaded and create a fresh instance each time.
    /// This means if you are using NCrunch you must ensure this runs in a new process each time
    /// </summary>
    [TestFixture, SingleThreaded, NonParallelizable]
    public class DogstatsdStaticApiTests
    {
        private UdpListener _udpListener;
        private Thread _listenThread;
        
        [OneTimeSetUp]
        public void SetUpUdpListener()
        {
            _udpListener = new UdpListener(hostname: "127.0.0.1", port: 8126);
            var metricsConfig = new StatsdConfig { StatsdServerName = "127.0.0.1", StatsdPort = 8126 };

            DogStatsd.Configure(metricsConfig);            
        }

        [OneTimeTearDown]
        public void TearDownUdpListener()
        {
            _udpListener.Dispose();
        }

        [SetUp]
        public void StartUdpListenerThread()
        {
            _listenThread = new Thread(new ParameterizedThreadStart(_udpListener.Listen));
            _listenThread.Start();
        }

        [TearDown]
        public void ClearUdpListenerMessages()
        {
            _udpListener.GetAndClearLastMessages(); // just to be sure that nothing is left over
        }

        [Test]
        public void distribution_should_be_received()
        {
            DogStatsd.Distribution("distribution", 42);
            AssertWasReceived("distribution:42|d");
        }

        // Test helper. Waits until the listener is done receiving a message,
        // then asserts that the passed string is equal to the message received.
        private void AssertWasReceived(string shouldBe, int index = 0)
        {
            // Stall until the the listener receives a message or times out
            while (_listenThread.IsAlive);
            Assert.AreEqual(shouldBe, _udpListener.GetAndClearLastMessages()[index]);
        }
    }
}