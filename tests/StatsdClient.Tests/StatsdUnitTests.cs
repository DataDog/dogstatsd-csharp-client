using System;
using System.Text;
using Moq;
using NUnit.Framework;

namespace StatsdClient.Tests
{
    [TestFixture]
    public class UnitTests
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
        public void send_increase_counter_by_x()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Counting, int>("counter", 5);
            Mock.Get(_udp).Verify(x => x.Send("counter:5|c"));
        }

        [Test]
        public void send_decrease_counter_by_x()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Counting, int>("counter", -5);
            Mock.Get(_udp).Verify(x => x.Send("counter:-5|c"));
        }

        [Test]
        public void send_increase_counter_by_x_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Counting, int>("counter", 5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("counter:5|c|#tag1:true,tag2"));
        }

        [Test]
        public void send_increase_counter_by_x_and_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Counting, int>("counter", 5, sampleRate: 0.1);
            Mock.Get(_udp).Verify(x => x.Send("counter:5|c|@0.1"));
        }

        [Test]
        public void send_increase_counter_by_x_and_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Counting, int>("counter", 5, sampleRate: 0.1, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("counter:5|c|@0.1|#tag1:true,tag2"));
        }

        [Test]
        public void send_increase_counter_counting_exception_fails_silently()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            Mock.Get(_udp).Setup(x => x.Send(It.IsAny<string>())).Throws<Exception>();
            s.Send<Statsd.Counting, int>("counter", 5);
        }

        [Test]
        public void add_increase_counter_by_x()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Counting, int>("counter", 5);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("counter:5|c"));
        }

        [Test]
        public void add_increase_counter_by_x_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Counting, int>("counter", 5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("counter:5|c|#tag1:true,tag2"));
        }

        [Test]
        public void add_increase_counter_by_x_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Counting, int>("counter", 5, sampleRate: 0.5);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("counter:5|c|@0.5"));
        }

        [Test]
        public void add_increase_counter_by_x_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Counting, int>("counter", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("counter:5|c|@0.5|#tag1:true,tag2"));
        }

        // =-=-=-=- TIMER -=-=-=-=

        [Test]
        public void send_timer()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Timing, int>("timer", 5);
            Mock.Get(_udp).Verify(x => x.Send("timer:5|ms"));
        }

        [Test]
        public void send_timer_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Timing, double>("timer", 5.5);
            Mock.Get(_udp).Verify(x => x.Send("timer:5.5|ms"));
        }

        [Test]
        public void send_timer_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Timing, int>("timer", 5, tags: new[] { "tag1:true" });
            Mock.Get(_udp).Verify(x => x.Send("timer:5|ms|#tag1:true"));
        }

        [Test]
        public void send_timer_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Timing, int>("timer", 5, sampleRate: 0.5);
            Mock.Get(_udp).Verify(x => x.Send("timer:5|ms|@0.5"));
        }

        [Test]
        public void send_timer_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Timing, int>("timer", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("timer:5|ms|@0.5|#tag1:true,tag2"));
        }

        [Test]
        public void send_timer_exception_fails_silently()
        {
            Mock.Get(_udp).Setup(x => x.Send(It.IsAny<string>())).Throws<Exception>();
            Statsd s = new Statsd(_udp);
            s.Send<Statsd.Timing, int>("timer", 5);
        }

        [Test]
        public void send_timer_with_lambda()
        {
            const string statName = "name";
            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send(() => testMethod(), statName);

            Mock.Get(_udp).Verify(x => x.Send("name:500|ms"));
        }

        [Test]
        public void send_timer_with_lambda_and_tags()
        {
            const string statName = "name";
            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send(() => testMethod(), statName, tags: new[] { "tag1:true", "tag2" });

            Mock.Get(_udp).Verify(x => x.Send("name:500|ms|#tag1:true,tag2"));
        }

        [Test]
        public void send_timer_with_lambda_and_sample_rate()
        {
            const string statName = "name";
            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send(() => testMethod(), statName, sampleRate: 1.1);

            Mock.Get(_udp).Verify(x => x.Send("name:500|ms|@1.1"));
        }

        [Test]
        public void send_timer_with_lambda_and_sample_rate_and_tags()
        {
            const string statName = "name";
            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send(() => testMethod(), statName, sampleRate: 1.1, tags: new[] { "tag1:true", "tag2" });

            Mock.Get(_udp).Verify(x => x.Send("name:500|ms|@1.1|#tag1:true,tag2"));
        }

        [Test]
        public void send_timer_with_lamba_still_records_on_error_and_still_bubbles_up_exception()
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
        public void send_timer_with_lambda_set_return_value_with()
        {
            const string statName = "name";
            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            int returnValue = 0;
            s.Send(() => returnValue = testMethod(), statName);

            Mock.Get(_udp).Verify(x => x.Send("name:500|ms"));
            Assert.That(returnValue, Is.EqualTo(5));
        }

        [Test]
        public void add_timer_with_lamba()
        {
            const string statName = "name";

            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add(() => testMethod(), statName);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("name:500|ms"));
        }

        [Test]
        public void add_timer_with_lamba_and_tags()
        {
            const string statName = "name";

            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add(() => testMethod(), statName, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("name:500|ms|#tag1:true,tag2"));
        }

        [Test]
        public void add_timer_with_lamba_and_sample_rate_and_tags()
        {
            const string statName = "name";

            IStopwatch stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add(() => testMethod(), statName, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("name:500|ms|@0.5|#tag1:true,tag2"));
        }

        [Test]
        public void add_timer_with_lamba_still_records_on_error_and_still_bubbles_up_exception()
        {
            const string statName = "name";

            var stopwatch = Mock.Of<IStopwatch>();
            Mock.Get(stopwatch).Setup(x => x.ElapsedMilliseconds()).Returns(500);
            Mock.Get(_stopwatch).Setup(x => x.Get()).Returns(stopwatch);

            var s = new Statsd(_udp, _randomGenerator, _stopwatch);

            Assert.Throws<InvalidOperationException>(() => s.Add(() => throw new InvalidOperationException(), statName));

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("name:500|ms"));
        }

        // =-=-=-=- GAUGE -=-=-=-=

        [Test]
        public void send_gauge()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Gauge, int>("gauge", 5);
            Mock.Get(_udp).Verify(x => x.Send("gauge:5|g"));
        }

        [Test]
        public void send_gauge_with_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Gauge, double>("gauge", 4.2);
            Mock.Get(_udp).Verify(x => x.Send("gauge:4.2|g"));
        }

        [Test]
        public void send_gauge_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Gauge, int>("gauge", 5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("gauge:5|g|#tag1:true,tag2"));
        }

        [Test]
        public void send_gauge_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Gauge, int>("gauge", 5, sampleRate: 0.5);
            Mock.Get(_udp).Verify(x => x.Send("gauge:5|g|@0.5"));
        }

        [Test]
        public void send_gauge_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Gauge, int>("gauge", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("gauge:5|g|@0.5|#tag1:true,tag2"));
        }

        [Test]
        public void send_gauge_with_sample_rate_and_tags_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Gauge, double>("gauge", 5.4, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("gauge:5.4|g|@0.5|#tag1:true,tag2"));
        }

        [Test]
        public void send_gauge_exception_fails_silently()
        {
            Mock.Get(_udp).Setup(x => x.Send(It.IsAny<string>())).Throws<Exception>();
            Statsd s = new Statsd(_udp);
            s.Send<Statsd.Gauge, int>("gauge", 5);
        }

        [Test]
        public void add_gauge()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Gauge, int>("gauge", 5);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("gauge:5|g"));
        }

        [Test]
        public void add_gauge_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Gauge, double>("gauge", 5.3);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("gauge:5.3|g"));
        }

        [Test]
        public void add_gauge_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Gauge, int>("gauge", 5, sampleRate: 0.5);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("gauge:5|g|@0.5"));
        }

        [Test]
        public void add_gauge_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Gauge, int>("gauge", 5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("gauge:5|g|#tag1:true,tag2"));
        }

        [Test]
        public void add_gauge_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Gauge, int>("gauge", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("gauge:5|g|@0.5|#tag1:true,tag2"));
        }

        [Test]
        public void add_gauge_with_sample_rate_and_tags_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Gauge, int>("gauge", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("gauge:5|g|@0.5|#tag1:true,tag2"));
        }

        // =-=-=-=- COMBINATION -=-=-=-=

        [Test]
        public void add_one_counter_and_one_gauge_shows_in_commands()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Counting, int>("counter", 1, 0.1);
            s.Add<Statsd.Timing, int>("timer", 1);

            Assert.That(s.Commands.Count, Is.EqualTo(2));
            Assert.That(s.Commands[0], Is.EqualTo("counter:1|c|@0.1"));
            Assert.That(s.Commands[1], Is.EqualTo("timer:1|ms"));
        }

        [Test]
        public void add_one_counter_and_one_gauge_with_no_sample_rate_shows_in_commands()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Counting, int>("counter", 1);
            s.Add<Statsd.Timing, int>("timer", 1);

            Assert.That(s.Commands.Count, Is.EqualTo(2));
            Assert.That(s.Commands[0], Is.EqualTo("counter:1|c"));
            Assert.That(s.Commands[1], Is.EqualTo("timer:1|ms"));
        }

        [Test]
        public void add_one_counter_and_one_gauge_sends_in_one_go()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Counting, int>("counter", 1, 0.1);
            s.Add<Statsd.Timing, int>("timer", 1);
            s.Send();

            Mock.Get(_udp).Verify(x => x.Send("counter:1|c|@0.1\ntimer:1|ms"));
        }

        [Test]
        public void add_one_counter_and_one_gauge_sends_and_removes_commands()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Counting, int>("counter", 1, 0.1);
            s.Add<Statsd.Timing, int>("timer", 1);
            s.Send();

            Assert.That(s.Commands.Count, Is.EqualTo(0));
        }

        [Test]
        public void add_one_counter_and_send_one_gauge_sends_only_sends_the_last_and_clears_queue()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Counting, int>("counter", 1);
            s.Send<Statsd.Timing, int>("timer", 1);

            Mock.Get(_udp).Verify(x => x.Send("timer:1|ms"));

            s.Send();

            Mock.Get(_udp).Verify(x => x.Send("counter:1|c"), Times.Never);
        }

        [Test]
        public void add_one_counter_and_send_one_gauge_sends_only_sends_the_last_one_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Counting, double>("counter", 1.1);
            s.Send<Statsd.Timing, int>("timer", 1);

            Mock.Get(_udp).Verify(x => x.Send("timer:1|ms"));
        }

        [Test]
        public void add_one_counter_and_send_one_gauge_sends_only_sends_the_last_two_doubles()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Counting, double>("counter", 1.1);
            s.Send<Statsd.Timing, double>("timer", 1.1);

            Mock.Get(_udp).Verify(x => x.Send("timer:1.1|ms"));
        }

        // =-=-=-=- EVENT -=-=-=-=
        //Event(string title, string text, string alertType = null, string aggregationKey = null, string sourceType = null, int? dateHappened = null, string priority = null, string hostname = null, string[] tags = null)

        [Test]
        public void send_event()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text");
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text"));
        }

        [Test]
        public void send_event_with_alertType()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", alertType: "warning");
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|t:warning"));
        }

        [Test]
        public void send_event_with_aggregationKey()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", aggregationKey: "key");
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|k:key"));
        }

        [Test]
        public void send_event_with_sourceType()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", sourceType: "source");
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|s:source"));
        }

        [Test]
        public void send_event_with_dateHappened()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", dateHappened: 123456);
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|d:123456"));
        }

        [Test]
        public void send_event_with_priority()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", priority: "low");
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|p:low"));
        }

        [Test]
        public void send_event_with_hostname()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", hostname: "hostname");
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|h:hostname"));
        }

        [Test]
        public void send_event_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", tags: new[] { "tag1", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|#tag1,tag2"));
        }

        [Test]
        public void send_event_with_message_that_is_too_long()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = 8 * 1024 - 16; //16 is the number of characters in the final message that is not the title
            var builder = BuildLongString(length);
            var title = builder;

            var exception = Assert.Throws<Exception>(() => s.Send(title + "x", "text"));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        [Test]
        public void send_event_with_truncation_for_title_that_is_too_long()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = 8 * 1024 - 16; //16 is the number of characters in the final message that is not the title
            var builder = BuildLongString(length);
            var title = builder;

            s.Send(title + "x", "text", truncateIfTooLong: true);
            var expected = string.Format("_e{{{0},4}}:{1}|text", length, title);
            Mock.Get(_udp).Verify(x => x.Send(expected));
        }

        [Test]
        public void send_event_with_truncation_for_text_that_is_too_long()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = 8 * 1024 - 17; //17 is the number of characters in the final message that is not the text
            var builder = BuildLongString(length);
            var text = builder;

            s.Send("title", text + "x", truncateIfTooLong: true);
            var expected = string.Format("_e{{5,{0}}}:title|{1}", length, text);
            Mock.Get(_udp).Verify(x => x.Send(expected));
        }

        [Test]
        public void send_event_with_statsd_truncation()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            // Enable truncation at Statsd level
            s.TruncateIfTooLong = true;

            var length = 8 * 1024 - 17; //17 is the number of characters in the final message that is not the text
            var builder = BuildLongString(length);
            var text = builder;

            s.Send("title", text + "x");
            var expected = string.Format("_e{{5,{0}}}:title|{1}", length, text);
            Mock.Get(_udp).Verify(x => x.Send(expected));
        }

        private static string BuildLongString(int length)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < length; i++)
                builder.Append(i % 10);
            return builder.ToString();
        }

        // =-=-=-=- PREFIX -=-=-=-=

        [Test]
        public void set_prefix_on_stats_name_when_calling_send()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch, "a.prefix.");
            s.Send<Statsd.Counting, int>("counter", 5);
            s.Send<Statsd.Counting, int>("counter", 5);

            Mock.Get(_udp).Verify(x => x.Send("a.prefix.counter:5|c"), Times.Exactly(2));
        }

        [Test]
        public void add_counter_sets_prefix_on_name()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch, "another.prefix.");

            s.Add<Statsd.Counting, int>("counter", 1, sampleRate: 0.1);
            s.Add<Statsd.Timing, int>("timer", 1);
            s.Send();

            Mock.Get(_udp).Verify(x => x.Send("another.prefix.counter:1|c|@0.1\nanother.prefix.timer:1|ms"));
        }

        private int testMethod()
        {
            return 5;
        }

        // DOGSTATSD-SPECIFIC

        // =-=-=-=- HISTOGRAM -=-=-=-=
        [Test]
        public void send_histogram()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Histogram, int>("histogram", 5);
            Mock.Get(_udp).Verify(x => x.Send("histogram:5|h"));
        }

        [Test]
        public void send_histogram_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Histogram, double>("histogram", 5.3);
            Mock.Get(_udp).Verify(x => x.Send("histogram:5.3|h"));
        }

        [Test]
        public void send_histogram_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Histogram, int>("histogram", 5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("histogram:5|h|#tag1:true,tag2"));
        }

        [Test]
        public void send_histogram_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Histogram, int>("histogram", 5, sampleRate: 0.5);
            Mock.Get(_udp).Verify(x => x.Send("histogram:5|h|@0.5"));
        }

        [Test]
        public void send_histogram_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Histogram, int>("histogram", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("histogram:5|h|@0.5|#tag1:true,tag2"));
        }

        [Test]
        public void add_histogram()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Histogram, int>("histogram", 5);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("histogram:5|h"));
        }

        [Test]
        public void add_histogram_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Histogram, double>("histogram", 5.3);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("histogram:5.3|h"));
        }

        [Test]
        public void add_histogram_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Histogram, int>("histogram", 5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("histogram:5|h|#tag1:true,tag2"));
        }

        [Test]
        public void add_histogram_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Histogram, int>("histogram", 5, 0.5);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("histogram:5|h|@0.5"));
        }

        [Test]
        public void add_histogram_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Histogram, int>("histogram", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("histogram:5|h|@0.5|#tag1:true,tag2"));
        }

        // =-=-=-=- DISTRIBUTION -=-=-=-=
        [Test]
        public void send_distribution()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Distribution, int>("distribution", 5);
            Mock.Get(_udp).Verify(x => x.Send("distribution:5|d"));
        }

        [Test]
        public void send_distribution_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Distribution, double>("distribution", 5.3);
            Mock.Get(_udp).Verify(x => x.Send("distribution:5.3|d"));
        }

        [Test]
        public void send_distribution_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Distribution, int>("distribution", 5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("distribution:5|d|#tag1:true,tag2"));
        }

        [Test]
        public void send_distribution_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Distribution, int>("distribution", 5, sampleRate: 0.5);
            Mock.Get(_udp).Verify(x => x.Send("distribution:5|d|@0.5"));
        }

        [Test]
        public void send_distribution_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Distribution, int>("distribution", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("distribution:5|d|@0.5|#tag1:true,tag2"));
        }

        [Test]
        public void add_distribution()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Distribution, int>("distribution", 5);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("distribution:5|d"));
        }

        [Test]
        public void add_distribution_double()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Distribution, double>("distribution", 5.3);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("distribution:5.3|d"));
        }

        [Test]
        public void add_distribution_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Distribution, int>("distribution", 5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("distribution:5|d|#tag1:true,tag2"));
        }

        [Test]
        public void add_distribution_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Distribution, int>("distribution", 5, 0.5);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("distribution:5|d|@0.5"));
        }

        [Test]
        public void add_distribution_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Distribution, int>("distribution", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("distribution:5|d|@0.5|#tag1:true,tag2"));
        }

        // =-=-=-=- SET -=-=-=-=
        [Test]
        public void send_set()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Set, int>("set", 5);
            Mock.Get(_udp).Verify(x => x.Send("set:5|s"));
        }

        [Test]
        public void send_set_string()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Set, string>("set", "objectname");
            Mock.Get(_udp).Verify(x => x.Send("set:objectname|s"));
        }

        [Test]
        public void send_set_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Set, int>("set", 5, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("set:5|s|#tag1:true,tag2"));
        }

        [Test]
        public void send_set_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Set, int>("set", 5, sampleRate: 0.1);
            Mock.Get(_udp).Verify(x => x.Send("set:5|s|@0.1"));
        }

        [Test]
        public void send_set_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Set, int>("set", 5, sampleRate: 0.1, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("set:5|s|@0.1|#tag1:true,tag2"));
        }

        [Test]
        public void send_set_string_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send<Statsd.Set, string>("set", "objectname", sampleRate: 0.1, tags: new[] { "tag1:true", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("set:objectname|s|@0.1|#tag1:true,tag2"));
        }

        [Test]
        public void add_set()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Set, int>("set", 5);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("set:5|s"));
        }

        [Test]
        public void add_set_string()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Set, string>("set", "string");

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("set:string|s"));
        }

        [Test]
        public void add_set_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Set, int>("set", 5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("set:5|s|#tag1:true,tag2"));
        }

        [Test]
        public void add_set_with_sample_rate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Set, int>("set", 5, sampleRate: 0.5);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("set:5|s|@0.5"));
        }

        [Test]
        public void add_set_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Set, int>("set", 5, sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("set:5|s|@0.5|#tag1:true,tag2"));
        }

        [Test]
        public void add_set_string_with_sample_rate_and_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add<Statsd.Set, string>("set", "string", sampleRate: 0.5, tags: new[] { "tag1:true", "tag2" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("set:string|s|@0.5|#tag1:true,tag2"));
        }

        // =-=-=-=- ServiceCheck -=-=-=-=
        [Test]
        public void send_service_check()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0);
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0"));
        }

        [Test]
        public void send_service_check_with_timestamp()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0, timestamp: 1);
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0|d:1"));
        }

        [Test]
        public void send_service_check_with_hostname()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0, hostname: "hostname");
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0|h:hostname"));
        }

        [Test]
        public void send_service_check_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0, tags: new[] { "tag1:value1", "tag2", "tag3:value3" });
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0|#tag1:value1,tag2,tag3:value3"));
        }

        [Test]
        public void send_service_check_with_message()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0, serviceCheckMessage: "message");
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0|m:message"));
        }

        [Test]
        public void send_service_check_with_pipe_in_name()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            Assert.Throws<ArgumentException>(() => s.Send("name|", 0));
        }

        [Test]
        [TestCase("\r\n")]
        [TestCase("\n")]
        public void send_service_check_with_new_line_in_name(string newline)
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name" + newline, 0);
            Mock.Get(_udp).Verify(x => x.Send("_sc|name\\n|0"));
        }

        [Test]
        public void send_service_check_with_suffix_in_message()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0, serviceCheckMessage: "m:message");
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0|m:m\\:message"));
        }

        [Test]
        public void send_service_check_with_all_optional()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("name", 0, 1, "hostname", new[] { "tag1:value1", "tag2", "tag3:value3" }, "message");
            Mock.Get(_udp).Verify(x => x.Send("_sc|name|0|d:1|h:hostname|#tag1:value1,tag2,tag3:value3|m:message"));
        }

        [Test]
        public void send_service_check_with_message_that_is_too_long()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = 8 * 1024 - 13;
            var builder = BuildLongString(length);
            var message = builder;

            var exception = Assert.Throws<Exception>(() => s.Send("name", 0, serviceCheckMessage: message + "x"));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        [Test]
        public void send_service_check_with_message_that_is_too_long_truncate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = 8 * 1024 - 13;
            var builder = BuildLongString(length);
            var message = builder;

            s.Send("name", 0, serviceCheckMessage: message + "x", truncateIfTooLong: true);

            var expected = "_sc|name|0|m:" + message;
            Mock.Get(_udp).Verify(x => x.Send(expected));
        }

        [Test]
        public void send_service_check_with_name_that_is_too_long_truncate()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = 8 * 1024 - 6;
            var builder = BuildLongString(length);
            var name = builder;

            var exception = Assert.Throws<ArgumentException>(() => s.Send(name + "x", 0, truncateIfTooLong: true));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        [Test]
        public void add_service_check()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0"));
        }

        [Test]
        public void add_service_check_with_timestamp()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0, timestamp: 1);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0|d:1"));
        }

        [Test]
        public void add_service_check_with_hostname()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0, hostname: "hostname");

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0|h:hostname"));
        }

        [Test]
        public void add_service_check_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0, tags: new[] { "tag1:value1", "tag2", "tag3:value3" });

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0|#tag1:value1,tag2,tag3:value3"));
        }

        [Test]
        public void add_service_check_with_message()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0, serviceCheckMessage: "message");

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0|m:message"));
        }

        [Test]
        public void add_service_check_with_pipe_in_name()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            Assert.Throws<ArgumentException>(() => s.Add("name|", 0));
        }

        [Test]
        [TestCase("\r\n")]
        [TestCase("\n")]
        public void add_service_check_with_new_line_in_name(string newline)
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name" + newline, 0);

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name\\n|0"));
        }

        [Test]
        public void add_service_check_with_suffix_in_message()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0, serviceCheckMessage: "m:message");

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0|m:m\\:message"));
        }

        [Test]
        public void add_service_check_with_all_optional()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Add("name", 0, 1, "hostname", new[] { "tag1:value1", "tag2", "tag3:value3" }, "message");

            Assert.That(s.Commands.Count, Is.EqualTo(1));
            Assert.That(s.Commands[0], Is.EqualTo("_sc|name|0|d:1|h:hostname|#tag1:value1,tag2,tag3:value3|m:message"));
        }

        [Test]
        public void add_service_check_with_message_that_is_too_long()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = 8 * 1024 - 13;
            var builder = BuildLongString(length);
            var message = builder;

            var exception = Assert.Throws<Exception>(() => s.Add("name", 0, serviceCheckMessage: message + "x"));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        [Test]
        public void add_service_check_with_name_that_is_too_long()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = 8 * 1024 - 6;
            var builder = BuildLongString(length);
            var name = builder;

            var exception = Assert.Throws<Exception>(() => s.Add(name + "x", 0));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }
    }
}
