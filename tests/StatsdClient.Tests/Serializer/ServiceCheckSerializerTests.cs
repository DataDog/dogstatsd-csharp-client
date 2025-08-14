using System;
using System.Text;
using NUnit.Framework;
using StatsdClient.Statistic;

namespace StatsdClient.Tests
{
    [TestFixture]
    public class ServiceCheckSerializerTests
    {
        [Test]
        public void SendServiceCheck()
        {
            AssertSerialize("_sc|name|0", "name", 0);
        }

        [Test]
        public void SendServiceCheckWithTimestamp()
        {
            AssertSerialize("_sc|name|0|d:1", "name", 0, timestamp: 1);
        }

        [Test]
        public void SendServiceCheckWithHostname()
        {
            AssertSerialize("_sc|name|0|h:hostname", "name", 0, hostname: "hostname");
        }

        [Test]
        public void SendServiceCheckWithTags()
        {
            AssertSerialize(
                "_sc|name|0|#tag1:value1,tag2,tag3:value3",
                "name",
                0,
                tags: new[] { "tag1:value1", "tag2", "tag3:value3" });
        }

        [Test]
        public void SendServiceCheckWithExternalData()
        {
            AssertSerialize("_sc|name|0|e:service-check-external-data|m:message", "name", 0, serviceCheckMessage: "message", externalData: "service-check-external-data");
        }

        [Test]
        public void SendServiceCheckWithContainerID()
        {
            AssertSerialize("_sc|name|0|c:container|m:message", "name", 0, serviceCheckMessage: "message", containerID: "container");
        }

        [Test]
        public void SendServiceCheckWithMessage()
        {
            AssertSerialize("_sc|name|0|m:message", "name", 0, serviceCheckMessage: "message");
        }

        [Test]
        public void SendServiceCheckWithPipeInName()
        {
            Assert.Throws<ArgumentException>(() => Serialize("name|", 0, null, null, null, null));
        }

        [Test]
        [TestCase("\r\n")]
        [TestCase("\n")]
        public void SendServiceCheckWithNewLineInName(string newline)
        {
            AssertSerialize("_sc|name\\n|0", "name" + newline, 0);
        }

        [Test]
        public void SendServiceCheckWithSuffixInMessage()
        {
            AssertSerialize("_sc|name|0|m:m\\:message", "name", 0, serviceCheckMessage: "m:message");
        }

        [Test]
        public void SendServiceCheckWithAllOptional()
        {
            AssertSerialize(
                "_sc|name|0|d:1|h:hostname|#tag1:value1,tag2,tag3:value3|m:message",
                "name",
                0,
                1,
                "hostname",
                new[] { "tag1:value1", "tag2", "tag3:value3" },
                "message");
        }

        [Test]
        public void SendServiceCheckWithMessageThatIsTooLong()
        {
            var length = (8 * 1024) - 13;
            var builder = BuildLongString(length);
            var message = builder;

            var exception = Assert.Throws<Exception>(() => Serialize("name", 0, null, null, null, message + "x"));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        [Test]
        public void SendServiceCheckWithMessageThatIsTooLongTruncate()
        {
            var length = (8 * 1024) - 13;
            var builder = BuildLongString(length);
            var message = builder;

            AssertSerialize("_sc|name|0|m:" + message, "name", 0, serviceCheckMessage: message + "x", truncateIfTooLong: true);
        }

        [Test]
        public void SendServiceCheckWithNameThatIsTooLongTruncate()
        {
            var length = (8 * 1024) - 6;
            var builder = BuildLongString(length);
            var name = builder;

            var exception = Assert.Throws<ArgumentException>(
                () => Serialize(name + "x", 0, null, null, null, null, truncateIfTooLong: true));
            Assert.That(exception.Message, Contains.Substring("payload is too big"));
        }

        [Test]
        public void SendServiceCheckWithCardinalityLow()
        {
            AssertSerialize("_sc|name|0|card:low", "name", 0, cardinality: Cardinality.Low);
        }

        [Test]
        public void SendServiceCheckWithCardinalityHigh()
        {
            AssertSerialize("_sc|name|0|card:high", "name", 0, cardinality: Cardinality.High);
        }

        [Test]
        public void SendServiceCheckWithCardinalityOrchestrator()
        {
            AssertSerialize("_sc|name|0|card:orchestrator", "name", 0, cardinality: Cardinality.Orchestrator);
        }

        [Test]
        public void SendServiceCheckWithCardinalityNone()
        {
            AssertSerialize("_sc|name|0|card:none", "name", 0, cardinality: Cardinality.None);
        }

        [Test]
        public void SendServiceCheckWithCardinalityAndTags()
        {
            AssertSerialize("_sc|name|0|#tag1,tag2|card:low", "name", 0, cardinality: Cardinality.Low, tags: new[] { "tag1", "tag2" });
        }

        [Test]
        public void SendServiceCheckWithCardinalityAndAllOptional()
        {
            AssertSerialize(
                "_sc|name|0|d:1|h:hostname|#tag1,tag2|card:high|m:message",
                "name",
                0,
                timestamp: 1,
                hostname: "hostname",
                cardinality: Cardinality.High,
                tags: new[] { "tag1", "tag2" },
                serviceCheckMessage: "message");
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
            string name,
            int status,
            int? timestamp = null,
            string hostname = null,
            string[] tags = null,
            string serviceCheckMessage = null,
            bool truncateIfTooLong = false,
            string externalData = null,
            string containerID = null,
            Cardinality? cardinality = null)
        {
            var serializedMetric = Serialize(name, status, timestamp, hostname, tags, serviceCheckMessage, truncateIfTooLong, externalData, containerID, cardinality);
            Assert.AreEqual(expectValue, serializedMetric.ToString());
        }

        private static SerializedMetric Serialize(
                    string name,
                    int status,
                    int? timestamp = null,
                    string hostname = null,
                    string[] tags = null,
                    string serviceCheckMessage = null,
                    bool truncateIfTooLong = false,
                    string externalData = null,
                    string containerID = null,
                    Cardinality? cardinality = null)
        {
            var statsServiceCheck = new StatsServiceCheck
            {
                Name = name,
                Status = status,
                Timestamp = timestamp,
                Hostname = hostname,
                ServiceCheckMessage = serviceCheckMessage,
                TruncateIfTooLong = truncateIfTooLong,
                Tags = tags,
                Cardinality = cardinality,
            };
            var serializer = CreateSerializer(externalData, containerID);
            var serializedMetric = new SerializedMetric();
            serializer.SerializeTo(ref statsServiceCheck, serializedMetric);
            return serializedMetric;
        }

        private static ServiceCheckSerializer CreateSerializer(string externalData, string containerID)
        {
            var originDetection = new OriginDetection(externalData, containerID);
            var serializerHelper = new SerializerHelper(null, originDetection);
            return new ServiceCheckSerializer(serializerHelper);
        }
    }
}
