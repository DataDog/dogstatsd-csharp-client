using System;
using System.Threading;
using NUnit.Framework;
using StatsdClient;
using Tests.Helpers;

namespace Tests
{
    [TestFixture]
    public class SynchronousModeIntegrationTests
    {
        private readonly int _serverPort = Convert.ToInt32("8127");
        private UdpListener _udpListener;
        private Thread _listenThread;
        private string _serverName = "127.0.0.1";
        private DogStatsdService _dogStatsdService;

        [OneTimeSetUp]
        public void SetupListener()
        {
            _udpListener = new UdpListener(_serverName, _serverPort);
        }

        [OneTimeTearDown]
        public void TearDownUdpListener()
        {
            _udpListener.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            _listenThread = new Thread(new ParameterizedThreadStart(_udpListener.Listen));
            _listenThread.Start();

            var config = new StatsdConfig
            {
                StatsdServerName = _serverName,
                StatsdPort = _serverPort,
                SynchronousMode = true,
                OriginDetection = false,
            };
            config.ClientSideAggregation = null;
            config.Advanced.TelemetryFlushInterval = null;
            _dogStatsdService = new DogStatsdService();
            _dogStatsdService.Configure(config);
        }

        [TearDown]
        public void Cleanup()
        {
            _udpListener.GetAndClearLastMessages();
            _dogStatsdService.Dispose();
        }

        [Test]
        public void Counter()
        {
            _dogStatsdService.Counter("sync.counter", 42);
            _dogStatsdService.Flush();
            AssertWasReceived("sync.counter:42|c");
        }

        [Test]
        public void Gauge()
        {
            _dogStatsdService.Gauge("sync.gauge", 100.5);
            _dogStatsdService.Flush();
            AssertWasReceived("sync.gauge:100.5|g");
        }

        [Test]
        public void Histogram()
        {
            _dogStatsdService.Histogram("sync.histogram", 250);
            _dogStatsdService.Flush();
            AssertWasReceived("sync.histogram:250|h");
        }

        [Test]
        public void Distribution()
        {
            _dogStatsdService.Distribution("sync.distribution", 30);
            _dogStatsdService.Flush();
            AssertWasReceived("sync.distribution:30|d");
        }

        [Test]
        public void Timer()
        {
            _dogStatsdService.Timer("sync.timer", 999);
            _dogStatsdService.Flush();
            AssertWasReceived("sync.timer:999|ms");
        }

        [Test]
        public void Set()
        {
            _dogStatsdService.Set("sync.set", "user123");
            _dogStatsdService.Flush();
            AssertWasReceived("sync.set:user123|s");
        }

        [Test]
        public void CounterWithTags()
        {
            _dogStatsdService.Counter("sync.counter", 1, tags: new[] { "env:prod", "region:us" });
            _dogStatsdService.Flush();
            AssertWasReceived("sync.counter:1|c|#env:prod,region:us");
        }

        [Test]
        public void Event()
        {
            _dogStatsdService.Event("Title", "Text");
            _dogStatsdService.Flush();
            AssertWasReceived("_e{5,4}:Title|Text");
        }

        [Test]
        public void ServiceCheck()
        {
            _dogStatsdService.ServiceCheck("my.check", Status.OK);
            _dogStatsdService.Flush();
            AssertWasReceived("_sc|my.check|0");
        }

        [Test]
        public void DisposeFlushesRemainingMetrics()
        {
            _dogStatsdService.Counter("sync.dispose.counter", 1);
            _dogStatsdService.Dispose();

            while (_listenThread.IsAlive)
            {
            }

            var messages = _udpListener.GetAndClearLastMessages();
            Assert.IsNotEmpty(messages);
            Assert.That(messages[0], Does.Contain("sync.dispose.counter:1|c"));
        }

        private void AssertWasReceived(string shouldBe, int index = 0)
        {
            while (_listenThread.IsAlive)
            {
            }

            Assert.AreEqual(shouldBe + "\n", _udpListener.GetAndClearLastMessages()[index]);
        }
    }
}
