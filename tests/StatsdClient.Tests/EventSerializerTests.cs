using System;
using System.Text;
using Moq;
using NUnit.Framework;

namespace StatsdClient.Tests
{
    [TestFixture]
    public class EventSerializerTests
    {
        [Test]
        public void Send_event()
        {
            AssertSerialize("_e{5,4}:title|text", "title", "text");
        }

        [Test]
        public void Send_event_with_alertType()
        {
            AssertSerialize("_e{5,4}:title|text|t:warning", "title", "text", alertType: "warning");
        }

        [Test]
        public void Send_event_with_aggregationKey()
        {
            AssertSerialize("_e{5,4}:title|text|k:key", "title", "text", aggregationKey: "key");
        }

        [Test]
        public void Send_event_with_sourceType()
        {
            AssertSerialize("_e{5,4}:title|text|s:source", "title", "text", sourceType: "source");
        }

        [Test]
        public void Send_event_with_dateHappened()
        {
            AssertSerialize("_e{5,4}:title|text|d:123456", "title", "text", dateHappened: 123456);
        }

        [Test]
        public void Send_event_with_priority()
        {
            AssertSerialize("_e{5,4}:title|text|p:low", "title", "text", priority: "low");
        }

        [Test]
        public void Send_event_with_hostname()
        {
            AssertSerialize("_e{5,4}:title|text|h:hostname", "title", "text", hostname: "hostname");
        }

        [Test]
        public void Send_event_with_tags()
        {
            AssertSerialize("_e{5,4}:title|text|#tag1,tag2", "title", "text", tags: new[] { "tag1", "tag2" });
        }

        [Test]
        public void Send_event_with_message_that_is_too_long()
        {
            var length = (8 * 1024) - 16; // 16 is the number of characters in the final message that is not the title
            var builder = BuildLongString(length);
            var title = builder;

            var serializer = CreateSerializer();
            var exception = Assert.Throws<Exception>(
                () => serializer.Serialize(title + "x", "text", null, null, null, null, null, null, null));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        [Test]
        public void Send_event_with_truncation_for_title_that_is_too_long()
        {
            var length = (8 * 1024) - 16; // 16 is the number of characters in the final message that is not the title
            var builder = BuildLongString(length);
            var title = builder;

            AssertSerialize(
                string.Format("_e{{{0},4}}:{1}|text", length, title),
                title + "x",
                "text",
                truncateIfTooLong: true);
        }

        [Test]
        public void Send_event_with_truncation_for_text_that_is_too_long()
        {
            var length = (8 * 1024) - 17; // 17 is the number of characters in the final message that is not the text
            var builder = BuildLongString(length);
            var text = builder;

            AssertSerialize(
                string.Format("_e{{5,{0}}}:title|{1}", length, text),
                "title",
                text + "x",
                truncateIfTooLong: true);
        }

        [Test]
        public void Send_event_with_statsd_truncation()
        {
            var length = (8 * 1024) - 17; // 17 is the number of characters in the final message that is not the text
            var builder = BuildLongString(length);
            var text = builder;

            AssertSerialize(
                string.Format("_e{{5,{0}}}:title|{1}", length, text),
                "title",
                text + "x",
                truncateIfTooLong: true);
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

        private static void AssertSerialize(
            string expectValue,
            string title = null,
            string text = null,
            string alertType = null,
            string aggregationKey = null,
            string sourceType = null,
            int? dateHappened = null,
            string priority = null,
            string hostname = null,
            string[] tags = null,
            bool truncateIfTooLong = false)
        {
            var serializer = CreateSerializer();
            var rawMetric = serializer.Serialize(
                title,
                text,
                alertType,
                aggregationKey,
                sourceType,
                dateHappened,
                priority,
                hostname,
                tags,
                truncateIfTooLong);
            Assert.AreEqual(expectValue, rawMetric);
        }

        private static EventSerializer CreateSerializer()
        {
            return new EventSerializer(null);
        }
    }
}
