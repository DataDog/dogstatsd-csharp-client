using NUnit.Framework;
using StatsdClient.Aggregator;
using StatsdClient.Statistic;

namespace StatsdClient.Tests.Aggregator
{
    [TestFixture]
    public class CountAggregatorTests
    {
        [Test]
        public void OnNewValue()
        {
            var handler = new BufferBuilderHandlerMock();
            var aggregator = new CountAggregator(MetricAggregatorParametersFactory.Create(handler.Object));
            AddStatsMetric(aggregator, "s1", 1);
            AddStatsMetric(aggregator, "s1", 2);
            AddStatsMetric(aggregator, "s2", 2);
            aggregator.TryFlush(force: true);
            Assert.AreEqual("s1:3|c|@0,s2:2|c|@0", handler.Value);

            AddStatsMetric(aggregator, "s3", 1);
            aggregator.TryFlush(force: true);
            Assert.AreEqual("s3:1|c|@0", handler.Value);
        }

        private static void AddStatsMetric(CountAggregator aggregator, string statName, double value)
        {
            var statsMetric = new StatsMetric
            {
                MetricType = MetricType.
                Count,
                StatName = statName,
                NumericValue = value,
            };

            aggregator.OnNewValue(ref statsMetric);
        }
    }
}