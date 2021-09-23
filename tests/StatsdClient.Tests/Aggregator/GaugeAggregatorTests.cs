using NUnit.Framework;
using StatsdClient.Aggregator;
using StatsdClient.Statistic;

namespace StatsdClient.Tests.Aggregator
{
    [TestFixture]
    public class GaugeAggregatorTests
    {
        [Test]
        public void OnNewValue()
        {
            var handler = new BufferBuilderHandlerMock();
            var aggregator = new GaugeAggregator(MetricAggregatorParametersFactory.Create(handler.Object));
            AddStatsMetric(aggregator, "s1", 1);
            AddStatsMetric(aggregator, "s1", 2);
            AddStatsMetric(aggregator, "s2", 3);
            aggregator.TryFlush(force: true);

            Assert.AreEqual("s1:2|g|@0\ns2:3|g|@0\n", handler.Value);
        }

        private static void AddStatsMetric(GaugeAggregator aggregator, string statName, double value)
        {
            var statsMetric = new StatsMetric
            {
                MetricType = MetricType.Gauge,
                StatName = statName,
                NumericValue = value,
            };

            aggregator.OnNewValue(ref statsMetric);
        }
    }
}