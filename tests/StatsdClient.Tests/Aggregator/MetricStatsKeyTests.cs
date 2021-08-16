using NUnit.Framework;
using StatsdClient.Aggregator;

namespace StatsdClient.Tests.Aggregator
{
    [TestFixture]
    public class MetricStatsKeyTests
    {
        [Test]
        public void Equals()
        {
            Assert.AreEqual(
                new MetricStatsKey("m1", new[] { "tag" }),
                new MetricStatsKey("m1", new[] { "tag" }));
            Assert.AreNotEqual(
                new MetricStatsKey("m1", new[] { "tag" }),
                new MetricStatsKey("m2", new[] { "tag" }));
            Assert.AreNotEqual(
                new MetricStatsKey("m1", new[] { "tag" }),
                new MetricStatsKey("m1", new[] { "tag2" }));
        }

        [Test]
        public void HashCode()
        {
            Assert.AreEqual(
                new MetricStatsKey("m1", new[] { "tag" }).GetHashCode(),
                new MetricStatsKey("m1", new[] { "tag" }).GetHashCode());
        }
    }
}