using System;
using System.Text;
using Moq;
using NUnit.Framework;

namespace StatsdClient.Tests
{
    [TestFixture]
    public class EventSerializerTests
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

        // =-=-=-=- EVENT -=-=-=-=
        // Event(string title, string text, string alertType = null, string aggregationKey = null, string sourceType = null, int? dateHappened = null, string priority = null, string hostname = null, string[] tags = null)

        [Test]
        public void Send_event()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text");
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text"));
        }

        [Test]
        public void Send_event_with_alertType()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", alertType: "warning");
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|t:warning"));
        }

        [Test]
        public void Send_event_with_aggregationKey()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", aggregationKey: "key");
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|k:key"));
        }

        [Test]
        public void Send_event_with_sourceType()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", sourceType: "source");
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|s:source"));
        }

        [Test]
        public void Send_event_with_dateHappened()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", dateHappened: 123456);
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|d:123456"));
        }

        [Test]
        public void Send_event_with_priority()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", priority: "low");
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|p:low"));
        }

        [Test]
        public void Send_event_with_hostname()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", hostname: "hostname");
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|h:hostname"));
        }

        [Test]
        public void Send_event_with_tags()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            s.Send("title", "text", tags: new[] { "tag1", "tag2" });
            Mock.Get(_udp).Verify(x => x.Send("_e{5,4}:title|text|#tag1,tag2"));
        }

        [Test]
        public void Send_event_with_message_that_is_too_long()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = (8 * 1024) - 16; // 16 is the number of characters in the final message that is not the title
            var builder = BuildLongString(length);
            var title = builder;

            var exception = Assert.Throws<Exception>(() => s.Send(title + "x", "text"));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        [Test]
        public void Send_event_with_truncation_for_title_that_is_too_long()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = (8 * 1024) - 16; // 16 is the number of characters in the final message that is not the title
            var builder = BuildLongString(length);
            var title = builder;

            s.Send(title + "x", "text", truncateIfTooLong: true);
            var expected = string.Format("_e{{{0},4}}:{1}|text", length, title);
            Mock.Get(_udp).Verify(x => x.Send(expected));
        }

        [Test]
        public void Send_event_with_truncation_for_text_that_is_too_long()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);

            var length = (8 * 1024) - 17; // 17 is the number of characters in the final message that is not the text
            var builder = BuildLongString(length);
            var text = builder;

            s.Send("title", text + "x", truncateIfTooLong: true);
            var expected = string.Format("_e{{5,{0}}}:title|{1}", length, text);
            Mock.Get(_udp).Verify(x => x.Send(expected));
        }

        [Test]
        public void Send_event_with_statsd_truncation()
        {
            Statsd s = new Statsd(_udp, _randomGenerator, _stopwatch);
            // Enable truncation at Statsd level
            s.TruncateIfTooLong = true;

            var length = (8 * 1024) - 17; // 17 is the number of characters in the final message that is not the text
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
            {
                builder.Append(i % 10);
            }

            return builder.ToString();
        }
    }
}
