using System;
using System.Text;
using Moq;
using NUnit.Framework;

namespace StatsdClient.Tests
{
    [TestFixture]
    public class MetricSerializerTests
    {
        private IStatsdUDP _udp;
        private IRandomGenerator _randomGenerator;
        private IStopWatchFactory _stopwatch;

        [SetUp]
        public void Setup()
        {
            _udp = Mock.Of<IStatsdUDP>();
            _randomGenerator = Mock.Of<IRandomGenerator>();
            Mock.Get(_randomGenerator).Setup(x => x.ShouldSend(It.IsAny<double>())).Returns(true);
            _stopwatch = Mock.Of<IStopWatchFactory>();
        }

        // =-=-=-=- COUNTER -=-=-=-=

        [Test]
        public void Send_increase_counter_by_x()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Counting, int>("counter", 5);
            Mock.Get(_udp).Verify(x => x.Send("counter:5|c"));
        }

        [Test]
        public void Send_decrease_counter_by_x()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Counting, int>("counter", -5);
            Mock.Get(_udp).Verify(x => x.Send("counter:-5|c"));
        }

        [Test]
        public void Send_increase_counter_by_x_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Counting, int>("counter", 5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("counter:5|c|#tag1:true,tag2"));
        }

        [Test]
        public void Send_increase_counter_by_x_and_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Counting, int>("counter", 5, sampleRate: 0.1);
            Mock.Get(_udp).Verify(x => x.Send("counter:5|c|@0.1"));
        }

        [Test]
        public void Send_increase_counter_by_x_and_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Counting, int>("counter", 5, sampleRate: 0.1, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("counter:5|c|@0.1|#tag1:true,tag2"));
        }

        [Test]
        public void Send_increase_counter_counting_exception_fails_silently()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            Mock.Get(_udp).Setup(x => x.Send(It.IsAny<string>())).Throws<Exception>();
            s.Send<Statsd.Counting, int>("counter", 5);
        }

        // =-=-=-=- TIMER -=-=-=-=

        [Test]
        public void Send_timer()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Timing, int>("timer", 5);
            Mock.Get(_udp).Verify(x => x.Send("timer:5|ms"));
        }

        [Test]
        public void Send_timer_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Timing, double>("timer", 5.5);
            Mock.Get(_udp).Verify(x => x.Send("timer:5.5|ms"));
        }

        [Test]
        public void Send_timer_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Timing, int>("timer", 5, tags: new[] { "tag1:true" });
            Mock.Get(_udp).Verify(x => x.Send("timer:5|ms|#tag1:true"));
        }

        [Test]
        public void Send_timer_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Timing, int>("timer", 5, sampleRate: 0.5);
            Mock.Get(_udp).Verify(x => x.Send("timer:5|ms|@0.5"));
        }

        [Test]
        public void Send_timer_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Timing, int>("timer", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("timer:5|ms|@0.5|#tag1:true,tag2"));
        }

        [Test]
        public void Send_timer_exception_fails_silently()
        {
            Mock.Get(_udp).Setup(x => x.Send(It.IsAny<string>())).Throws<Exception>();
            Statsd s = new Statsd(_udp);
            s.Send<Statsd.Timing, int>("timer", 5);
        }

        [Test]
        public void Send_timer_with_lambda()
        {
            const string statName = "name";
            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send(() => TestMethod(), statName);

            Mock.Get(_udp).Verify(x => x.Send("name:500|ms"));
        }

        [Test]
        public void Send_timer_with_lambda_and_tags()
        {
            const string statName = "name";
            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send(() => TestMethod(), statName, tags: new[] { "tag1:true", "tag2" });

            Mock.Get(_udp).Verify(x => x.Send("name:500|ms|#tag1:true,tag2"));
        }

        [Test]
        public void Send_timer_with_lambda_and_sample_rate()
        {
            const string statName = "name";
            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send(() => TestMethod(), statName, sampleRate: 1.1);

            Mock.Get(_udp).Verify(x => x.Send("name:500|ms|@1.1"));
        }

        [Test]
        public void Send_timer_with_lambda_and_sample_rate_and_tags()
        {
            const string statName = "name";
            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send(() => TestMethod(), statName, sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" });

            Mock.Get(_udp).Verify(x => x.Send("name:500|ms|@1.1|#tag1:true,tag2"));
        }

        [Test]
        public void Send_timer_with_lamba_still_records_on_error_and_still_bubbles_up_exception()
        {
            const string statName = "name";
            var stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            var s = new Statsd(_udp, _randomGenerator, _stopwatch);
            Assert.Throws<InvalidOperationException>(() => s.Send(() => throw new InvalidOperationException(), statName));

            Mock.Get(_udp).Verify(x => x.Send("name:500|ms"));
        }

        [Test]
        public void Send_timer_with_lambda_set_return_value_with()
        {
            const string statName = "name";
            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            int returnValue = 0;
            s.Send(() => returnValue = TestMethod(), statName);

            Mock.Get(_udp).Verify(x => x.Send("name:500|ms"));
            Assert.That(returnValue, Is.EqualTo(5));
        }

        // =-=-=-=- GAUGE -=-=-=-=

        [Test]
        public void Send_gauge()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Gauge, int>("gauge", 5);
            Mock.Get(_udp).Verify(x => x.Send("gauge:5|g"));
        }

        [Test]
        public void Send_gauge_with_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Gauge, double>("gauge", 4.2);
            Mock.Get(_udp).Verify(x => x.Send("gauge:4.2|g"));
        }

        [Test]
        public void Send_gauge_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Gauge, int>("gauge", 5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("gauge:5|g|#tag1:true,tag2"));
        }

        [Test]
        public void Send_gauge_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Gauge, int>("gauge", 5, sampleRate: 0.5);
            Mock.Get(_udp).Verify(x => x.Send("gauge:5|g|@0.5"));
        }

        [Test]
        public void Send_gauge_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Gauge, int>("gauge", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("gauge:5|g|@0.5|#tag1:true,tag2"));
        }

        [Test]
        public void Send_gauge_with_sample_rate_and_tags_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Gauge, double>("gauge", 5.4, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("gauge:5.4|g|@0.5|#tag1:true,tag2"));
        }

        [Test]
        public void Send_gauge_exception_fails_silently()
        {
            Mock.Get(_udp).Setup(x => x.Send(It.IsAny<string>())).Throws<Exception>();
            Statsd s = new Statsd(_udp);
            s.Send<Statsd.Gauge, int>("gauge", 5);
        }

        // =-=-=-=- PREFIX -=-=-=-=

        [Test]
        public void Set_prefix_on_stats_name_when_calling_send()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch, "a.prefix.");
            s.Send<Statsd.Counting, int>("counter", 5);
            s.Send<Statsd.Counting, int>("counter", 5);

            Mock.Get(_udp).Verify(x => x.Send("a.prefix.counter:5|c"), Times.Exactly(2));
        }

        // DOGSTATSD-SPECIFIC

        // =-=-=-=- HISTOGRAM -=-=-=-=
        [Test]
        public void Send_histogram()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Histogram, int>("histogram", 5);
            Mock.Get(_udp).Verify(x => x.Send("histogram:5|h"));
        }

        [Test]
        public void Send_histogram_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Histogram, double>("histogram", 5.3);
            Mock.Get(_udp).Verify(x => x.Send("histogram:5.3|h"));
        }

        [Test]
        public void Send_histogram_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Histogram, int>("histogram", 5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("histogram:5|h|#tag1:true,tag2"));
        }

        [Test]
        public void Send_histogram_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Histogram, int>("histogram", 5, sampleRate: 0.5);
            Mock.Get(_udp).Verify(x => x.Send("histogram:5|h|@0.5"));
        }

        [Test]
        public void Send_histogram_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Histogram, int>("histogram", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("histogram:5|h|@0.5|#tag1:true,tag2"));
        }

        // =-=-=-=- DISTRIBUTION -=-=-=-=
        [Test]
        public void Send_distribution()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Distribution, int>("distribution", 5);
            Mock.Get(_udp).Verify(x => x.Send("distribution:5|d"));
        }

        [Test]
        public void Send_distribution_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Distribution, double>("distribution", 5.3);
            Mock.Get(_udp).Verify(x => x.Send("distribution:5.3|d"));
        }

        [Test]
        public void Send_distribution_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Distribution, int>("distribution", 5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("distribution:5|d|#tag1:true,tag2"));
        }

        [Test]
        public void Send_distribution_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Distribution, int>("distribution", 5, sampleRate: 0.5);
            Mock.Get(_udp).Verify(x => x.Send("distribution:5|d|@0.5"));
        }

        [Test]
        public void Send_distribution_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Distribution, int>("distribution", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("distribution:5|d|@0.5|#tag1:true,tag2"));
        }

        // =-=-=-=- SET -=-=-=-=
        [Test]
        public void Send_set()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Set, int>("set", 5);
            Mock.Get(_udp).Verify(x => x.Send("set:5|s"));
        }

        [Test]
        public void Send_set_string()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Set, string>("set", "objectname");
            Mock.Get(_udp).Verify(x => x.Send("set:objectname|s"));
        }

        [Test]
        public void Send_set_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Set, int>("set", 5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("set:5|s|#tag1:true,tag2"));
        }

        [Test]
        public void Send_set_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Set, int>("set", 5, sampleRate: 0.1);
            Mock.Get(_udp).Verify(x => x.Send("set:5|s|@0.1"));
        }

        [Test]
        public void Send_set_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Set, int>("set", 5, sampleRate: 0.1, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("set:5|s|@0.1|#tag1:true,tag2"));
        }

        [Test]
        public void Send_set_string_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Set, string>("set", "objectname", sampleRate: 0.1, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("set:objectname|s|@0.1|#tag1:true,tag2"));
        }

        private static string BuildLongString(int length)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                builder.Append(i % 10);
            }

            return builder.ToString();
        }

        private int TestMethod()
        {
            return 5;
        }
    }
}
