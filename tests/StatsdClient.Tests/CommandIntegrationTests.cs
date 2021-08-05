using System;
using System.Threading;
using NUnit.Framework;
using StatsdClient;
using Tests.Helpers;

namespace Tests
{
    [TestFixture(false)]
    [TestFixture(true)] // Use client side aggregation.
    public class CommandIntegrationTests
    {
        private readonly int _serverPort = Convert.ToInt32("8126");
        private readonly ClientSideAggregationConfig _optionalClientSideAggregationConfig = null;
        private UdpListener _udpListener;
        private Thread _listenThread;
        private string serverName = "127.0.0.1";
        private DogStatsdService _dogStatsdService;

        public CommandIntegrationTests(bool useClientSideAggregation)
        {
            if (useClientSideAggregation)
            {
                _optionalClientSideAggregationConfig = new ClientSideAggregationConfig();
            }

            _udpListener = new UdpListener(serverName, _serverPort);
        }

        // When using client side aggregation, ignore tests using sample rate as client side aggregation
        // set the sample rate at 1.0 for count metrics.
        private bool IgnoreSampleRateTest => _optionalClientSideAggregationConfig != null;

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
            var metricsConfig = new StatsdConfig { StatsdServerName = serverName, StatsdPort = _serverPort };
            metricsConfig.ClientSideAggregation = _optionalClientSideAggregationConfig;
            metricsConfig.Advanced.TelemetryFlushInterval = TimeSpan.FromDays(1);
            _dogStatsdService = new DogStatsdService();
            _dogStatsdService.Configure(metricsConfig);
        }

        [TearDown]
        public void ClearUdpListenerMessages()
        {
            _udpListener.GetAndClearLastMessages(); // just to be sure that nothing is left over
            _dogStatsdService.Dispose();
        }

        [Test]
        public void Counter()
        {
            _dogStatsdService.Counter("counter", 1337);
            AssertWasReceived("counter:1337|c");
        }

        [Test]
        public void Counter_tags()
        {
            _dogStatsdService.Counter("counter", 1, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("counter:1|c|#tag1:true,tag2");
        }

        [Test]
        public void Counter_sample_rate()
        {
            if (!IgnoreSampleRateTest)
            {
                // A sample rate over 1 doesn't really make sense, but it allows
                // the test to pass every time
                _dogStatsdService.Counter("counter", 1, sampleRate: 1.1);
                AssertWasReceived("counter:1|c|@1.1");
            }
        }

        [Test]
        public void Counter_sample_rate_tags()
        {
            if (!IgnoreSampleRateTest)
            {
                _dogStatsdService.Counter("counter", 1337, sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
                AssertWasReceived("counter:1337|c|@12.2|#tag1:true,tag2");
            }
        }

        [Test]
        public void Counter_sample_rate_tags_double()
        {
            if (!IgnoreSampleRateTest)
            {
                _dogStatsdService.Counter("counter", 1337.3, sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
                AssertWasReceived("counter:1337.3|c|@12.2|#tag1:true,tag2");
            }
        }

        [Test]
        public void Increment()
        {
            _dogStatsdService.Increment("increment");
            AssertWasReceived("increment:1|c");
        }

        [Test]
        public void Increment_tags()
        {
            _dogStatsdService.Increment("increment", tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("increment:1|c|#tag1:true,tag2");
        }

        [Test]
        public void Increment_sample_rate()
        {
            if (!IgnoreSampleRateTest)
            {
                _dogStatsdService.Increment("increment", sampleRate: 1.1);
                AssertWasReceived("increment:1|c|@1.1");
            }
        }

        [Test]
        public void Increment_sample_rate_tags()
        {
            if (!IgnoreSampleRateTest)
            {
                _dogStatsdService.Increment("increment", sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
                AssertWasReceived("increment:1|c|@12.2|#tag1:true,tag2");
            }
        }

        [Test]
        public void Decrement()
        {
            _dogStatsdService.Decrement("decrement");
            AssertWasReceived("decrement:-1|c");
        }

        [Test]
        public void Decrement_tags()
        {
            _dogStatsdService.Decrement("decrement", tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("decrement:-1|c|#tag1:true,tag2");
        }

        [Test]
        public void Decrement_sample_rate()
        {
            if (!IgnoreSampleRateTest)
            {
                _dogStatsdService.Decrement("decrement", sampleRate: 1.1);
                AssertWasReceived("decrement:-1|c|@1.1");
            }
        }

        [Test]
        public void Decrement_sample_rate_tags()
        {
            if (!IgnoreSampleRateTest)
            {
                _dogStatsdService.Decrement("decrement", sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
                AssertWasReceived("decrement:-1|c|@12.2|#tag1:true,tag2");
            }
        }

        [Test]
        public void Gauge()
        {
            _dogStatsdService.Gauge("gauge", 1337);
            AssertWasReceived("gauge:1337|g");
        }

        [Test]
        public void Gauge_tags()
        {
            _dogStatsdService.Gauge("gauge", 1337, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("gauge:1337|g|#tag1:true,tag2");
        }

        [Test]
        public void Gauge_sample_rate()
        {
            _dogStatsdService.Gauge("gauge", 1337, sampleRate: 1.1);
            AssertWasReceived("gauge:1337|g|@1.1");
        }

        [Test]
        public void Gauge_sample_rate_tags()
        {
            _dogStatsdService.Gauge("gauge", 1337, sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("gauge:1337|g|@1.1|#tag1:true,tag2");
        }

        [Test]
        public void Gauge_double()
        {
            _dogStatsdService.Gauge("gauge", 6.3);
            AssertWasReceived("gauge:6.3|g");
        }

        [Test]
        public void Gauge_double_tags()
        {
            _dogStatsdService.Gauge("gauge", 3.1337, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("gauge:3.1337|g|#tag1:true,tag2");
        }

        [Test]
        public void Gauge_double_sample_rate()
        {
            _dogStatsdService.Gauge("gauge", 3.1337, sampleRate: 1.1);
            AssertWasReceived("gauge:3.1337|g|@1.1");
        }

        [Test]
        public void Gauge_double_sample_rate_tags()
        {
            _dogStatsdService.Gauge("gauge", 3.1337, sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("gauge:3.1337|g|@1.1|#tag1:true,tag2");
        }

        [Test]
        public void Gauge_double_rounding()
        {
            _dogStatsdService.Gauge("gauge", 1.0 / 9);
#if NET5_0
            // double formating changed in .NET Core 3.0
            AssertWasReceived("gauge:0.1111111111111111|g");
#else
            AssertWasReceived("gauge:0.111111111111111|g");
#endif
        }

        [Test]
        public void Histogram()
        {
            _dogStatsdService.Histogram("histogram", 42);
            AssertWasReceived("histogram:42|h");
        }

        [Test]
        public void Histogram_tags()
        {
            _dogStatsdService.Histogram("histogram", 42, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("histogram:42|h|#tag1:true,tag2");
        }

        [Test]
        public void Histogram_sample_rate()
        {
            _dogStatsdService.Histogram("histogram", 42, sampleRate: 1.1);
            AssertWasReceived("histogram:42|h|@1.1");
        }

        [Test]
        public void Histogram_sample_rate_tags()
        {
            _dogStatsdService.Histogram("histogram", 42, sampleRate: 1.1, tags: new[] { "tag1:true,tag2" });
            AssertWasReceived("histogram:42|h|@1.1|#tag1:true,tag2");
        }

        [Test]
        public void Histogram_double()
        {
            _dogStatsdService.Histogram("histogram", 42.1);
            AssertWasReceived("histogram:42.1|h");
        }

        [Test]
        public void Histogram_double_tags()
        {
            _dogStatsdService.Histogram("histogram", 42.1, tags: new[] { "tag1:true,tag2" });
            AssertWasReceived("histogram:42.1|h|#tag1:true,tag2");
        }

        [Test]
        public void Histogram_double_sample_rate()
        {
            _dogStatsdService.Histogram("histogram", 42.1, 1.1);
            AssertWasReceived("histogram:42.1|h|@1.1");
        }

        [Test]
        public void Histogram_double_sample_rate_tags()
        {
            _dogStatsdService.Histogram("histogram", 42.1, sampleRate: 1.1, tags: new[] { "tag1:true,tag2" });
            AssertWasReceived("histogram:42.1|h|@1.1|#tag1:true,tag2");
        }

        [Test]
        public void Set()
        {
            _dogStatsdService.Set("set", 42);
            AssertWasReceived("set:42|s");
        }

        [Test]
        public void Set_tags()
        {
            _dogStatsdService.Set("set", 42, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("set:42|s|#tag1:true,tag2");
        }

        [Test]
        public void Set_sample_rate()
        {
            _dogStatsdService.Set("set", 42, sampleRate: 1.1);
            AssertWasReceived("set:42|s|@1.1");
        }

        [Test]
        public void Set_sample_rate_tags()
        {
            _dogStatsdService.Set("set", 42, sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("set:42|s|@12.2|#tag1:true,tag2");
        }

        [Test]
        public void Set_double()
        {
            _dogStatsdService.Set("set", 42.2);
            AssertWasReceived("set:42.2|s");
        }

        [Test]
        public void Set_double_tags()
        {
            _dogStatsdService.Set("set", 42.2, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("set:42.2|s|#tag1:true,tag2");
        }

        [Test]
        public void Set_double_sample_rate()
        {
            _dogStatsdService.Set("set", 42.2, sampleRate: 1.1);
            AssertWasReceived("set:42.2|s|@1.1");
        }

        [Test]
        public void Set_double_sample_rate_tags()
        {
            _dogStatsdService.Set("set", 42.2, sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("set:42.2|s|@12.2|#tag1:true,tag2");
        }

        [Test]
        public void Set_string()
        {
            _dogStatsdService.Set("set", "string");
            AssertWasReceived("set:string|s");
        }

        [Test]
        public void Set_string_tags()
        {
            _dogStatsdService.Set("set", "string", tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("set:string|s|#tag1:true,tag2");
        }

        [Test]
        public void Set_string_sample_rate()
        {
            _dogStatsdService.Set("set", "string", sampleRate: 1.1);
            AssertWasReceived("set:string|s|@1.1");
        }

        [Test]
        public void Set_string_sample_rate_tags()
        {
            _dogStatsdService.Set("set", "string", sampleRate: 12.2, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("set:string|s|@12.2|#tag1:true,tag2");
        }

        [Test]
        public void Timer()
        {
            _dogStatsdService.Timer("someevent", 999);
            AssertWasReceived("someevent:999|ms");
        }

        [Test]
        public void Timer_tags()
        {
            _dogStatsdService.Timer("someevent", 999, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("someevent:999|ms|#tag1:true,tag2");
        }

        [Test]
        public void Timer_sample_rate()
            {
                _dogStatsdService.Timer("someevent", 999, sampleRate: 1.1);
                AssertWasReceived("someevent:999|ms|@1.1");
            }

        [Test]
        public void Timer_sample_rate_tags()
            {
                _dogStatsdService.Timer("someevent", 999, sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" });
                AssertWasReceived("someevent:999|ms|@1.1|#tag1:true,tag2");
            }

        [Test]
        public void Timer_double()
        {
            _dogStatsdService.Timer("someevent", 999.99);
            AssertWasReceived("someevent:999.99|ms");
        }

        [Test]
        public void Timer_double_tags()
        {
            _dogStatsdService.Timer("someevent", 999.99, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("someevent:999.99|ms|#tag1:true,tag2");
        }

        [Test]
        public void Timer_double_sample_rate()
            {
                _dogStatsdService.Timer("someevent", 999.99, sampleRate: 1.1);
                AssertWasReceived("someevent:999.99|ms|@1.1");
            }

        [Test]
        public void Timer_double_sample_rate_tags()
            {
                _dogStatsdService.Timer("someevent", 999.99, sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" });
                AssertWasReceived("someevent:999.99|ms|@1.1|#tag1:true,tag2");
            }

        [Test]
        public void Timer_method()
        {
            _dogStatsdService.Time(() => Thread.Sleep(100), "timer");
            // Make sure that the received timer is of the right order of magnitude.
            // The measured value will probably be a few ms longer than the sleep value.
            AssertWasReceivedMatches(@"timer:\d{3}\|ms");
        }

        [Test]
        public void Timer_method_tags()
        {
            _dogStatsdService.Time(() => Thread.Sleep(100), "timer", tags: new[] { "tag1:true", "tag2" });
            // Make sure that the received timer is of the right order of magnitude.
            // The measured value will probably be a few ms longer than the sleep value.
            AssertWasReceivedMatches(@"timer:\d{3}\|ms\|#tag1:true,tag2");
        }

        [Test]
        public void Timer_method_sample_rate()
            {
                _dogStatsdService.Time(() => Thread.Sleep(100), "timer", sampleRate: 1.1);
                // Make sure that the received timer is of the right order of magnitude.
                // The measured value will probably be a few ms longer than the sleep value.
                AssertWasReceivedMatches(@"timer:\d{3}\|ms\|@1\.1");
            }

        [Test]
        public void Timer_method_sample_rate_tags()
            {
                _dogStatsdService.Time(() => Thread.Sleep(100), "timer", sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" });
                // Make sure that the received timer is of the right order of magnitude.
                // The measured value will probably be a few ms longer than the sleep value.
                AssertWasReceivedMatches(@"timer:\d{3}\|ms\|@1\.1\|#tag1:true,tag2");
            }

        [Test]
        public void Timer_method_sets_return_value()
        {
            var returnValue = _dogStatsdService.Time(PauseAndReturnInt, "lifetheuniverseandeverything");
            AssertWasReceivedMatches(@"lifetheuniverseandeverything:\d{3}\|ms");
            Assert.AreEqual(42, returnValue);
        }

        [Test]
        public void Timer_method_sets_return_value_tags()
        {
            var returnValue = _dogStatsdService.Time(PauseAndReturnInt, "lifetheuniverseandeverything", tags: new[] { "towel:present" });
            AssertWasReceivedMatches(@"lifetheuniverseandeverything:\d{3}\|ms\|#towel:present");
            Assert.AreEqual(42, returnValue);
        }

        [Test]
        public void Timer_method_sets_return_value_sample_rate()
            {
                var returnValue = _dogStatsdService.Time(PauseAndReturnInt, "lifetheuniverseandeverything", sampleRate: 4.2);
                AssertWasReceivedMatches(@"lifetheuniverseandeverything:\d{3}\|ms\|@4\.2");
                Assert.AreEqual(42, returnValue);
            }

        [Test]
        public void Timer_method_sets_return_value_sample_rate_and_tag()
            {
                var returnValue = _dogStatsdService.Time(PauseAndReturnInt, "lifetheuniverseandeverything", sampleRate: 4.2, tags: new[] { "fjords" });
                AssertWasReceivedMatches(@"lifetheuniverseandeverything:\d{3}\|ms\|@4\.2\|#fjords");
                Assert.AreEqual(42, returnValue);
            }

        [Test]
        public void Timer_method_doesnt_swallow_exception_and_submits_metric()
        {
            Assert.Throws<Exception>(() => _dogStatsdService.Time(ThrowException, "somebadcode"));
            AssertWasReceivedMatches(@"somebadcode:\d{3}\|ms");
        }

        [Test]
        public void Timer_block()
        {
            using (_dogStatsdService.StartTimer("timer"))
            {
                Thread.Sleep(50);
                Thread.Sleep(60);
            }

            AssertWasReceivedMatches(@"timer:\d{3}\|ms");
        }

        [Test]
        public void Timer_block_tags()
        {
            using (_dogStatsdService.StartTimer("timer", tags: new[] { "tag1:true", "tag2" }))
            {
                Thread.Sleep(50);
                Thread.Sleep(60);
            }

            AssertWasReceivedMatches(@"timer:\d{3}\|ms\|#tag1:true,tag2");
        }

        [Test]
        public void Timer_block_sampleRate()
        {
            using (_dogStatsdService.StartTimer("timer", sampleRate: 1.1))
            {
                Thread.Sleep(50);
                Thread.Sleep(60);
            }

            AssertWasReceivedMatches(@"timer:\d{3}\|ms\|@1\.1");
        }

        [Test]
        public void Timer_block_sampleRate_and_tag()
        {
            using (_dogStatsdService.StartTimer("timer", sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" }))
            {
                Thread.Sleep(50);
                Thread.Sleep(60);
            }

            AssertWasReceivedMatches(@"timer:\d{3}\|ms\|@1\.1\|#tag1:true,tag2");
        }

        [Test]
        public void Timer_block_doesnt_swallow_exception_and_submits_metric()
        {
            // (Wasn't able to get this working with Assert.Throws)
            try
            {
                using (_dogStatsdService.StartTimer("timer"))
                {
                    ThrowException();
                }

                Assert.Fail();
            }
            catch (Exception)
            {
                AssertWasReceivedMatches(@"timer:\d{3}\|ms");
            }
        }

        [Test]
        public void Events_priority_and_date()
        {
            _dogStatsdService.Event("Title", "L1\r\nL2", priority: "low", dateHappened: 1375296969);
            AssertWasReceived("_e{5,6}:Title|L1\\nL2|d:1375296969|p:low");
        }

        [Test]
        public void Events_aggregation_key_and_tags()
        {
            _dogStatsdService.Event("Title♬", "♬ †øU †øU ¥ºu T0µ ♪", aggregationKey: "key", tags: new[] { "t1", "t2:v2" });
            AssertWasReceived("_e{8,32}:Title♬|♬ †øU †øU ¥ºu T0µ ♪|k:key|#t1,t2:v2");
        }

        [Test]
        public void Service_check_timestamp_hostname()
        {
            _dogStatsdService.ServiceCheck("na\r\nme", Status.OK, timestamp: 1375296969, hostname: "hostname");
            AssertWasReceived("_sc|na\\nme|0|d:1375296969|h:hostname");
        }

        [Test]
        public void Service_check_tags_message()
        {
            _dogStatsdService.ServiceCheck("na\r\nme", Status.CRITICAL, tags: new[] { "t1", "t2:v2" }, message: "m:mess\r\nage");
            AssertWasReceived("_sc|na\\nme|2|#t1,t2:v2|m:m\\:mess\\nage");
        }

        [Test]
        public void Distribution()
        {
            _dogStatsdService.Distribution("distribution", 42);
            AssertWasReceived("distribution:42|d");
        }

        [Test]
        public void Distribution_tags()
        {
            _dogStatsdService.Distribution("distribution", 42, tags: new[] { "tag1:true", "tag2" });
            AssertWasReceived("distribution:42|d|#tag1:true,tag2");
        }

        [Test]
        public void Distribution_sample_rate()
        {
            _dogStatsdService.Distribution("distribution", 42, sampleRate: 1.1);
            AssertWasReceived("distribution:42|d|@1.1");
        }

        [Test]
        public void Distribution_sample_rate_tags()
        {
            _dogStatsdService.Distribution("distribution", 42, sampleRate: 1.1, tags: new[] { "tag1:true,tag2" });
            AssertWasReceived("distribution:42|d|@1.1|#tag1:true,tag2");
        }

        [Test]
        public void Distribution_double()
        {
            _dogStatsdService.Distribution("distribution", 42.1);
            AssertWasReceived("distribution:42.1|d");
        }

        [Test]
        public void Distribution_double_tags()
        {
            _dogStatsdService.Distribution("distribution", 42.1, tags: new[] { "tag1:true,tag2" });
            AssertWasReceived("distribution:42.1|d|#tag1:true,tag2");
        }

        [Test]
        public void Distribution_double_sample_rate()
        {
            _dogStatsdService.Distribution("distribution", 42.1, 1.1);
            AssertWasReceived("distribution:42.1|d|@1.1");
        }

        [Test]
        public void Distribution_double_sample_rate_tags()
        {
            _dogStatsdService.Distribution("distribution", 42.1, sampleRate: 1.1, tags: new[] { "tag1:true,tag2" });
            AssertWasReceived("distribution:42.1|d|@1.1|#tag1:true,tag2");
        }

        // Test helper. Waits until the listener is done receiving a message,
        // then asserts that the passed string is equal to the message received.
        private void AssertWasReceived(string shouldBe, int index = 0)
        {
            // Stall until the the listener receives a message or times out
            _dogStatsdService.Dispose();
            while (_listenThread.IsAlive)
            {
            }

            Assert.AreEqual(shouldBe, _udpListener.GetAndClearLastMessages()[index]);
        }

        // Test helper. Waits until the listener is done receiving a message,
        // then asserts that the passed regular expression matches the received message.
        private void AssertWasReceivedMatches(string pattern, int index = 0)
        {
            // Stall until the the listener receives a message or times out
            while (_listenThread.IsAlive)
            {
            }

            StringAssert.IsMatch(pattern, _udpListener.GetAndClearLastMessages()[index]);
        }

        // [Helper]
        private int PauseAndReturnInt()
        {
            Thread.Sleep(100);
            return 42;
        }

        // [Helper]
        private int ThrowException()
        {
            Thread.Sleep(100);
            throw new Exception("test exception");
        }
    }
}