using System.Text;
using NUnit.Framework;
using StatsdClient;

namespace Tests
{
    [TestFixture]
    public class SerializerHelperTests
    {
        [Test]
        public void AppendTags()
        {
            var helper = new SerializerHelper(null);
            var builder = new StringBuilder();

            helper.AppendTags(builder, new string[0]);
            Assert.AreEqual(string.Empty, builder.ToString());

            helper.AppendTags(builder, new[] { "tag1", "tag2" });
            Assert.AreEqual("|#tag1,tag2", builder.ToString());
        }

        [Test]
        public void AppendTagsWithConstantTags()
        {
            var helper = new SerializerHelper(new[] { "ctag1", "ctag2" });
            var builder = new StringBuilder();

            helper.AppendTags(builder, new string[0]);
            Assert.AreEqual("|#ctag1,ctag2", builder.ToString());
            builder.Clear();

            helper.AppendTags(builder, new[] { "tag1", "tag2" });
            Assert.AreEqual("|#ctag1,ctag2,tag1,tag2", builder.ToString());
        }

        [Test]
        public void TruncateOverage()
        {
            Assert.AreEqual("123", SerializerHelper.TruncateOverage("12345", 2));
        }

        [Test]
        public void AppendIfNotNull()
        {
            var builder = new StringBuilder();

            SerializerHelper.AppendIfNotNull(builder, "prefix:", null);
            Assert.AreEqual(string.Empty, builder.ToString());

            SerializerHelper.AppendIfNotNull(builder, "prefix:", "value");
            Assert.AreEqual("prefix:value", builder.ToString());
        }

        [Test]
        public void GetSerializedMetric()
        {
            var serializerHelper = new SerializerHelper(null);

            var metric = serializerHelper.GetSerializedMetric();
            metric.Builder.Append("Test");
            Assert.AreNotEqual(metric, serializerHelper.GetSerializedMetric());
            metric.Dispose();

            var recycledMetric = serializerHelper.GetSerializedMetric();
            Assert.AreEqual(metric, recycledMetric);
            Assert.AreEqual(0, recycledMetric.Builder.Length);
        }
    }
}