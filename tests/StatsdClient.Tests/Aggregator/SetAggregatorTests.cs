using System;
using NUnit.Framework;
using StatsdClient.Aggregator;
using StatsdClient.Statistic;

namespace StatsdClient.Tests.Aggregator
{
    [TestFixture]
    public class SetAggregatorTests
    {
        [Test]
        public void OnNewValue()
        {
            var handler = new BufferBuilderHandlerMock();
            var aggregator = new SetAggregator(MetricAggregatorParametersFactory.Create(handler.Object), null);
            AddStatsMetric(aggregator, "s1", "1");
            AddStatsMetric(aggregator, "s1", "2");
            AddStatsMetric(aggregator, "s1", "2");
            AddStatsMetric(aggregator, "s1", "2");
            AddStatsMetric(aggregator, "s2", "3");
            aggregator.TryFlush(force: true);
            Assert.AreEqual("s1:1|s|@0,s1:2|s|@0,s2:3|s|@0", handler.Value);

            AddStatsMetric(aggregator, "s1", "4");
            aggregator.TryFlush(force: true);
            Assert.AreEqual("s1:4|s|@0", handler.Value);
        }

        [Test]
        public void Pool()
        {
            var handler = new BufferBuilderHandlerMock();
            var parameters = MetricAggregatorParametersFactory.Create(handler.Object, TimeSpan.FromMinutes(1), 1);
            var aggregator = new SetAggregator(parameters, null);

            // Check StatsMetricSet instance go back to the pool
            for (int i = 0; i < 10; ++i)
            {
                AddStatsMetric(aggregator, "s1", i.ToString());
                Assert.AreEqual($"s1:{i}|s|@0", handler.Value);
            }
        }

        private static void AddStatsMetric(SetAggregator aggregator, string statName, string value)
        {
            var statsMetric = new StatsMetric
            {
                MetricType = MetricType.Set,
                StatName = statName,
                StringValue = value,
            };

            aggregator.OnNewValue(ref statsMetric);
        }
    }
}