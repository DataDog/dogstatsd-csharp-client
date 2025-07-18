﻿using System;
using System.Text;
using Moq;
using NUnit.Framework;
using StatsdClient.Statistic;

namespace StatsdClient.Tests
{
    [TestFixture]
    public class EventSerializerTests
    {
        [Test]
        public void SendEvent()
        {
            AssertSerialize("_e{5,4}:title|text", "title", "text");
        }

        [Test]
        public void SendEventWithAlertType()
        {
            AssertSerialize("_e{5,4}:title|text|t:warning", "title", "text", alertType: "warning");
        }

        [Test]
        public void SendEventWithAggregationKey()
        {
            AssertSerialize("_e{5,4}:title|text|k:key", "title", "text", aggregationKey: "key");
        }

        [Test]
        public void SendEventWithSourceType()
        {
            AssertSerialize("_e{5,4}:title|text|s:source", "title", "text", sourceType: "source");
        }

        [Test]
        public void SendEventWithDateHappened()
        {
            AssertSerialize("_e{5,4}:title|text|d:123456", "title", "text", dateHappened: 123456);
        }

        [Test]
        public void SendEventWithPriority()
        {
            AssertSerialize("_e{5,4}:title|text|p:low", "title", "text", priority: "low");
        }

        [Test]
        public void SendEventWithHostname()
        {
            AssertSerialize("_e{5,4}:title|text|h:hostname", "title", "text", hostname: "hostname");
        }

        [Test]
        public void SendEventWithTags()
        {
            AssertSerialize("_e{5,4}:title|text|#tag1,tag2", "title", "text", tags: new[] { "tag1", "tag2" });
        }

        [Test]
        public void SendEventWithExternalData()
        {
            AssertSerialize("_e{5,4}:title|text|e:event-external-data", "title", "text", externalData: "event-external-data");
        }

        [Test]
        public void SendEventWithMessageThatIsTooLong()
        {
            var length = (8 * 1024) - 16; // 16 is the number of characters in the final message that is not the title
            var builder = BuildLongString(length);
            var title = builder;

            var serializer = CreateSerializer();
            var exception = Assert.Throws<Exception>(
                () =>
                {
                    var statsEvent = new StatsEvent { Title = title + "x", Text = "Text" };
                    serializer.SerializeTo(ref statsEvent, new SerializedMetric());
                });
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        [Test]
        public void SendEventWithTruncationForTitleThatIsTooLong()
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
        public void SendEventWithTruncationForTextThatIsTooLong()
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
        public void SendEventWithStatsdTruncation()
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
            bool truncateIfTooLong = false,
            string externalData = null)
        {
            var serializer = CreateSerializer(externalData);
            var statsEvent = new StatsEvent
            {
                Title = title,
                Text = text,
                AlertType = alertType,
                AggregationKey = aggregationKey,
                SourceType = sourceType,
                DateHappened = dateHappened,
                Priority = priority,
                Hostname = hostname,
                TruncateIfTooLong = truncateIfTooLong,
                Tags = tags,
            };

            var serializedMetric = new SerializedMetric();
            serializer.SerializeTo(ref statsEvent, serializedMetric);
            Assert.AreEqual(expectValue, serializedMetric.ToString());
        }

        private static EventSerializer CreateSerializer(string externalData = null)
        {
            var originDetection = externalData != null ? new OriginDetection(externalData) : null;
            var serializerHelper = new SerializerHelper(null, originDetection);
            return new EventSerializer(serializerHelper);
        }
    }
}
