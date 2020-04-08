using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;
using Tests.Helpers;


namespace Tests
{
    [TestFixture]
    public class CommandIntegrationTests
    {
        private UdpListener _udpListener;
        private Thread _listenThread;
        private readonly int _serverPort = Convert.ToInt32("8126");
        private string serverName = "127.0.0.1";
        private DogStatsdService _dogStatsdService;

        [OneTimeSetUp]
        public void SetUpUdpListener()
        {
            _udpListener = new UdpListener(serverName, _serverPort);
            var metricsConfig = new StatsdConfig { StatsdServerName = serverName, StatsdPort = _serverPort };
            _dogStatsdService = new DogStatsdService();
            _dogStatsdService.Configure(metricsConfig);
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

        // Test helper. Waits until the listener is done receiving a message,
        // then asserts that the passed string is equal to the message received.
        private void AssertWasReceived(string shouldBe, int index = 0)
        {
            // Stall until the the listener receives a message or times out
            while (_listenThread.IsAlive) ;
            Assert.AreEqual(shouldBe, _udpListener.GetAndClearLastMessages()[index]);
        }

        // Test helper. Waits until the listener is done receiving a message,
        // then asserts that the passed regular expression matches the received message.
        private void AssertWasReceivedMatches(string pattern, int index = 0)
        {
            // Stall until the the listener receives a message or times out
            while (_listenThread.IsAlive) ;
            StringAssert.IsMatch(pattern, _udpListener.GetAndClearLastMessages()[index]);

        }

        [Test]
        public void _udp_listener_sanity_test()
        {
            var client = new StatsdUDP("127.0.0.1",
                                       Convert.ToInt32("8126"));
            client.Send("iamnotinsane!");
            AssertWasReceived("iamnotinsane!");
        }

        [Test]
        public void counter()
        {
            _dogStatsdService.Counter("counter", 1337);
            AssertWasReceived("counter:1337|c");
        }


        [Test]
        public void counter_tags()
        {
            _dogStatsdService.Counter("counter", 1, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("counter:1|c|#tag1:true,tag2");
        }

        [Test]
        public void counter_sample_rate()
        {
            // A sample rate over 1 doesn't really make sense, but it allows
            // the test to pass every time
            _dogStatsdService.Counter("counter", 1, sampleRate: 1.1);
            AssertWasReceived("counter:1|c|@1.1");
        }

        [Test]
        public void counter_sample_rate_tags()
        {
            _dogStatsdService.Counter("counter", 1337, sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("counter:1337|c|@12.2|#tag1:true,tag2");
        }

        [Test]
        public void counter_sample_rate_tags_double()
        {
            _dogStatsdService.Counter("counter", 1337.3, sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("counter:1337.3|c|@12.2|#tag1:true,tag2");
        }

        [Test]
        public void increment()
        {
            _dogStatsdService.Increment("increment");
            AssertWasReceived("increment:1|c");
        }

        [Test]
        public void increment_tags()
        {
            _dogStatsdService.Increment("increment", tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("increment:1|c|#tag1:true,tag2");
        }

        [Test]
        public void increment_sample_rate()
        {
            _dogStatsdService.Increment("increment", sampleRate: 1.1);
            AssertWasReceived("increment:1|c|@1.1");
        }

        [Test]
        public void increment_sample_rate_tags()
        {
            _dogStatsdService.Increment("increment", sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("increment:1|c|@12.2|#tag1:true,tag2");
        }

        [Test]
        public void decrement()
        {
            _dogStatsdService.Decrement("decrement");
            AssertWasReceived("decrement:-1|c");
        }

        [Test]
        public void decrement_tags()
        {
            _dogStatsdService.Decrement("decrement", tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("decrement:-1|c|#tag1:true,tag2");
        }

        [Test]
        public void decrement_sample_rate()
        {
            _dogStatsdService.Decrement("decrement", sampleRate: 1.1);
            AssertWasReceived("decrement:-1|c|@1.1");
        }

        [Test]
        public void decrement_sample_rate_tags()
        {
            _dogStatsdService.Decrement("decrement", sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("decrement:-1|c|@12.2|#tag1:true,tag2");
        }

        [Test]
        public void gauge()
        {
            _dogStatsdService.Gauge("gauge", 1337);
            AssertWasReceived("gauge:1337|g");
        }

        [Test]
        public void gauge_tags()
        {
            _dogStatsdService.Gauge("gauge", 1337, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("gauge:1337|g|#tag1:true,tag2");
        }

        [Test]
        public void gauge_sample_rate()
        {
            _dogStatsdService.Gauge("gauge", 1337, sampleRate: 1.1);
            AssertWasReceived("gauge:1337|g|@1.1");
        }

        [Test]
        public void gauge_sample_rate_tags()
        {
            _dogStatsdService.Gauge("gauge", 1337, sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("gauge:1337|g|@1.1|#tag1:true,tag2");
        }

        [Test]
        public void gauge_double()
        {
            _dogStatsdService.Gauge("gauge", 6.3);
            AssertWasReceived("gauge:6.3|g");
        }

        [Test]
        public void gauge_double_tags()
        {
            _dogStatsdService.Gauge("gauge", 3.1337, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("gauge:3.1337|g|#tag1:true,tag2");
        }

        [Test]
        public void gauge_double_sample_rate()
        {
            _dogStatsdService.Gauge("gauge", 3.1337, sampleRate: 1.1);
            AssertWasReceived("gauge:3.1337|g|@1.1");
        }

        [Test]
        public void gauge_double_sample_rate_tags()
        {
            _dogStatsdService.Gauge("gauge", 3.1337, sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("gauge:3.1337|g|@1.1|#tag1:true,tag2");
        }

        [Test]
        public void gauge_double_rounding()
        {
            _dogStatsdService.Gauge("gauge", (double)1 / 9);
            AssertWasReceived("gauge:0.111111111111111|g");
        }

        [Test]
        public void histogram()
        {
            _dogStatsdService.Histogram("histogram", 42);
            AssertWasReceived("histogram:42|h");
        }

        [Test]
        public void histogram_tags()
        {
            _dogStatsdService.Histogram("histogram", 42, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("histogram:42|h|#tag1:true,tag2");
        }

        [Test]
        public void histogram_sample_rate()
        {
            _dogStatsdService.Histogram("histogram", 42, sampleRate: 1.1);
            AssertWasReceived("histogram:42|h|@1.1");
        }

        [Test]
        public void histogram_sample_rate_tags()
        {
            _dogStatsdService.Histogram("histogram", 42, sampleRate: 1.1, tags: new[] { "tag1:true,tag2" });
            AssertWasReceived("histogram:42|h|@1.1|#tag1:true,tag2");
        }

        [Test]
        public void histogram_double()
        {
            _dogStatsdService.Histogram("histogram", 42.1);
            AssertWasReceived("histogram:42.1|h");
        }

        [Test]
        public void histogram_double_tags()
        {
            _dogStatsdService.Histogram("histogram", 42.1, tags: new[] { "tag1:true,tag2" });
            AssertWasReceived("histogram:42.1|h|#tag1:true,tag2");
        }

        [Test]
        public void histogram_double_sample_rate()
        {
            _dogStatsdService.Histogram("histogram", 42.1, 1.1);
            AssertWasReceived("histogram:42.1|h|@1.1");
        }

        [Test]
        public void histogram_double_sample_rate_tags()
        {
            _dogStatsdService.Histogram("histogram", 42.1, sampleRate: 1.1, tags: new[] { "tag1:true,tag2" });
            AssertWasReceived("histogram:42.1|h|@1.1|#tag1:true,tag2");
        }



        [Test]
        public void set()
        {
            _dogStatsdService.Set("set", 42);
            AssertWasReceived("set:42|s");
        }

        [Test]
        public void set_tags()
        {
            _dogStatsdService.Set("set", 42, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("set:42|s|#tag1:true,tag2");
        }

        [Test]
        public void set_sample_rate()
        {
            _dogStatsdService.Set("set", 42, sampleRate: 1.1);
            AssertWasReceived("set:42|s|@1.1");
        }

        [Test]
        public void set_sample_rate_tags()
        {
            _dogStatsdService.Set("set", 42, sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("set:42|s|@12.2|#tag1:true,tag2");
        }

        [Test]
        public void set_double()
        {
            _dogStatsdService.Set("set", 42.2);
            AssertWasReceived("set:42.2|s");
        }

        [Test]
        public void set_double_tags()
        {
            _dogStatsdService.Set("set", 42.2, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("set:42.2|s|#tag1:true,tag2");
        }

        [Test]
        public void set_double_sample_rate()
        {
            _dogStatsdService.Set("set", 42.2, sampleRate: 1.1);
            AssertWasReceived("set:42.2|s|@1.1");
        }

        [Test]
        public void set_double_sample_rate_tags()
        {
            _dogStatsdService.Set("set", 42.2, sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("set:42.2|s|@12.2|#tag1:true,tag2");
        }

        [Test]
        public void set_string()
        {
            _dogStatsdService.Set("set", "string");
            AssertWasReceived("set:string|s");
        }

        [Test]
        public void set_string_tags()
        {
            _dogStatsdService.Set("set", "string", tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("set:string|s|#tag1:true,tag2");
        }

        [Test]
        public void set_string_sample_rate()
        {
            _dogStatsdService.Set("set", "string", sampleRate: 1.1);
            AssertWasReceived("set:string|s|@1.1");
        }

        [Test]
        public void set_string_sample_rate_tags()
        {
            _dogStatsdService.Set("set", "string", sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("set:string|s|@12.2|#tag1:true,tag2");
        }

        [Test]
        public void timer()
        {
            _dogStatsdService.Timer("someevent", 999);
            AssertWasReceived("someevent:999|ms");
        }

        [Test]
        public void timer_tags()
        {
            _dogStatsdService.Timer("someevent", 999, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("someevent:999|ms|#tag1:true,tag2");
        }

        [Test]
        public void timer_sample_rate()
        {
            _dogStatsdService.Timer("someevent", 999, sampleRate: 1.1);
            AssertWasReceived("someevent:999|ms|@1.1");
        }

        [Test]
        public void timer_sample_rate_tags()
        {
            _dogStatsdService.Timer("someevent", 999, sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("someevent:999|ms|@1.1|#tag1:true,tag2");
        }

        [Test]
        public void timer_double()
        {
            _dogStatsdService.Timer("someevent", 999.99);
            AssertWasReceived("someevent:999.99|ms");
        }

        [Test]
        public void timer_double_tags()
        {
            _dogStatsdService.Timer("someevent", 999.99, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("someevent:999.99|ms|#tag1:true,tag2");
        }

        [Test]
        public void timer_double_sample_rate()
        {
            _dogStatsdService.Timer("someevent", 999.99, sampleRate: 1.1);
            AssertWasReceived("someevent:999.99|ms|@1.1");
        }

        [Test]
        public void timer_double_sample_rate_tags()
        {
            _dogStatsdService.Timer("someevent", 999.99, sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("someevent:999.99|ms|@1.1|#tag1:true,tag2");
        }

        [Test]
        public void timer_method()
        {
            _dogStatsdService.Time(() => Thread.Sleep(100), "timer");
            // Make sure that the received timer is of the right order of magnitude.
            // The measured value will probably be a few ms longer than the sleep value.
            AssertWasReceivedMatches(@"timer:\d{3}\|ms");
        }

        [Test]
        public void timer_method_tags()
        {
            _dogStatsdService.Time(() => Thread.Sleep(100), "timer", tags: new[] { "tag1:true", "tag2" });
            // Make sure that the received timer is of the right order of magnitude.
            // The measured value will probably be a few ms longer than the sleep value.
            AssertWasReceivedMatches(@"timer:\d{3}\|ms\|#tag1:true,tag2");
        }

        [Test]
        public void timer_method_sample_rate()
        {
            _dogStatsdService.Time(() => Thread.Sleep(100), "timer", sampleRate: 1.1);
            // Make sure that the received timer is of the right order of magnitude.
            // The measured value will probably be a few ms longer than the sleep value.
            AssertWasReceivedMatches(@"timer:\d{3}\|ms\|@1\.1");
        }

        [Test]
        public void timer_method_sample_rate_tags()
        {
            _dogStatsdService.Time(() => Thread.Sleep(100), "timer", sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" });
            // Make sure that the received timer is of the right order of magnitude.
            // The measured value will probably be a few ms longer than the sleep value.
            AssertWasReceivedMatches(@"timer:\d{3}\|ms\|@1\.1\|#tag1:true,tag2");
        }

        // [Helper]
        private int pauseAndReturnInt()
        {
            Thread.Sleep(100);
            return 42;
        }

        [Test]
        public void timer_method_sets_return_value()
        {
            var returnValue = _dogStatsdService.Time(pauseAndReturnInt, "lifetheuniverseandeverything");
            AssertWasReceivedMatches(@"lifetheuniverseandeverything:\d{3}\|ms");
            Assert.AreEqual(42, returnValue);
        }

        [Test]
        public void timer_method_sets_return_value_tags()
        {
            var returnValue = _dogStatsdService.Time(pauseAndReturnInt, "lifetheuniverseandeverything", tags: new[] { "towel:present" });
            AssertWasReceivedMatches(@"lifetheuniverseandeverything:\d{3}\|ms\|#towel:present");
            Assert.AreEqual(42, returnValue);
        }

        [Test]
        public void timer_method_sets_return_value_sample_rate()
        {
            var returnValue = _dogStatsdService.Time(pauseAndReturnInt, "lifetheuniverseandeverything", sampleRate: 4.2);
            AssertWasReceivedMatches(@"lifetheuniverseandeverything:\d{3}\|ms\|@4\.2");
            Assert.AreEqual(42, returnValue);
        }

        [Test]
        public void timer_method_sets_return_value_sample_rate_and_tag()
        {
            var returnValue = _dogStatsdService.Time(pauseAndReturnInt, "lifetheuniverseandeverything", sampleRate: 4.2, tags: new[] { "fjords" });
            AssertWasReceivedMatches(@"lifetheuniverseandeverything:\d{3}\|ms\|@4\.2\|#fjords");
            Assert.AreEqual(42, returnValue);
        }

        // [Helper]
        private int throwException()
        {
            Thread.Sleep(100);
            throw new Exception("test exception");
        }

        [Test]
        public void timer_method_doesnt_swallow_exception_and_submits_metric()
        {
            Assert.Throws<Exception>(() => _dogStatsdService.Time(throwException, "somebadcode"));
            AssertWasReceivedMatches(@"somebadcode:\d{3}\|ms");
        }

        [Test]
        public void timer_block()
        {
            using (_dogStatsdService.StartTimer("timer"))
            {
                Thread.Sleep(50);
                Thread.Sleep(60);
            }

            AssertWasReceivedMatches(@"timer:\d{3}\|ms");
        }

        [Test]
        public void timer_block_tags()
        {
            using (_dogStatsdService.StartTimer("timer", tags: new[] { "tag1:true", "tag2" }))
            {
                Thread.Sleep(50);
                Thread.Sleep(60);
            }

            AssertWasReceivedMatches(@"timer:\d{3}\|ms\|#tag1:true,tag2");
        }

        [Test]
        public void timer_block_sampleRate()
        {
            using (_dogStatsdService.StartTimer("timer", sampleRate: 1.1))
            {
                Thread.Sleep(50);
                Thread.Sleep(60);
            }

            AssertWasReceivedMatches(@"timer:\d{3}\|ms\|@1\.1");
        }

        [Test]
        public void timer_block_sampleRate_and_tag()
        {
            using (_dogStatsdService.StartTimer("timer", sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" }))
            {
                Thread.Sleep(50);
                Thread.Sleep(60);
            }

            AssertWasReceivedMatches(@"timer:\d{3}\|ms\|@1\.1\|#tag1:true,tag2");
        }


        [Test]
        public void timer_block_doesnt_swallow_exception_and_submits_metric()
        {
            // (Wasn't able to get this working with Assert.Throws)
            try
            {
                using (_dogStatsdService.StartTimer("timer"))
                {
                    throwException();
                }

                Assert.Fail();
            }
            catch (Exception)
            {
                AssertWasReceivedMatches(@"timer:\d{3}\|ms");
            }
        }

        [Test]
        public void events_priority_and_date()
        {
            _dogStatsdService.Event("Title", "L1\r\nL2", priority: "low", dateHappened: 1375296969);
            AssertWasReceived("_e{5,6}:Title|L1\\nL2|d:1375296969|p:low");
        }

        [Test]
        public void events_aggregation_key_and_tags()
        {
            _dogStatsdService.Event("Title", "♬ †øU †øU ¥ºu T0µ ♪", aggregationKey: "key", tags: new[] { "t1", "t2:v2" });
            AssertWasReceived("_e{5,19}:Title|♬ †øU †øU ¥ºu T0µ ♪|k:key|#t1,t2:v2");
        }

        [Test]
        public void service_check_timestamp_hostname()
        {
            _dogStatsdService.ServiceCheck("na\r\nme", Status.OK, timestamp: 1375296969, hostname: "hostname");
            AssertWasReceived("_sc|na\\nme|0|d:1375296969|h:hostname");
        }

        [Test]
        public void service_check_tags_message()
        {
            _dogStatsdService.ServiceCheck("na\r\nme", Status.CRITICAL, tags: new[] { "t1", "t2:v2" }, message: "m:mess\r\nage");
            AssertWasReceived("_sc|na\\nme|2|#t1,t2:v2|m:m\\:mess\\nage");
        }

        #region Distrubution

        [Test]
        public void distribution()
        {
            _dogStatsdService.Distribution("distribution", 42);
            AssertWasReceived("distribution:42|d");
        }

        [Test]
        public void distribution_tags()
        {
            _dogStatsdService.Distribution("distribution", 42, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("distribution:42|d|#tag1:true,tag2");
        }


        [Test]
        public void distribution_sample_rate()
        {
            _dogStatsdService.Distribution("distribution", 42, sampleRate: 1.1);
            AssertWasReceived("distribution:42|d|@1.1");
        }

        [Test]
        public void distribution_sample_rate_tags()
        {
            _dogStatsdService.Distribution("distribution", 42, sampleRate: 1.1, tags: new[] { "tag1:true,tag2" });
            AssertWasReceived("distribution:42|d|@1.1|#tag1:true,tag2");
        }

        [Test]
        public void distribution_double()
        {
            _dogStatsdService.Distribution("distribution", 42.1);
            AssertWasReceived("distribution:42.1|d");
        }

        [Test]
        public void distribution_double_tags()
        {
            _dogStatsdService.Distribution("distribution", 42.1, tags: new[] { "tag1:true,tag2" });
            AssertWasReceived("distribution:42.1|d|#tag1:true,tag2");
        }

        [Test]
        public void distribution_double_sample_rate()
        {
            _dogStatsdService.Distribution("distribution", 42.1, 1.1);
            AssertWasReceived("distribution:42.1|d|@1.1");
        }

        [Test]
        public void distribution_double_sample_rate_tags()
        {
            _dogStatsdService.Distribution("distribution", 42.1, sampleRate: 1.1, tags: new[] { "tag1:true,tag2" });
            AssertWasReceived("distribution:42.1|d|@1.1|#tag1:true,tag2");
        }

        #endregion
    }
}

