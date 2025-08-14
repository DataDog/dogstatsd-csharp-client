using System;
using System.Linq;
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

        [Test]
        public void AggregatesByCardinality()
        {
            var handler = new BufferBuilderHandlerMock();
            var aggregator = new GaugeAggregator(MetricAggregatorParametersFactory.Create(handler.Object));

            // Add metrics with same name but different cardinalities
            // Gauge aggregation keeps the last value per unique key
            AddStatsMetric(aggregator, "cpu_usage", 10.0, Cardinality.Low);
            AddStatsMetric(aggregator, "cpu_usage", 20.0, Cardinality.Low); // Should replace above (last value wins)
            AddStatsMetric(aggregator, "cpu_usage", 30.0, Cardinality.High); // Different cardinality - should NOT replace above
            AddStatsMetric(aggregator, "cpu_usage", 40.0, null); // Different cardinality (null) - should NOT replace above

            aggregator.TryFlush(force: true);

            var output = handler.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries).OrderBy(s => s).ToArray();

            Assert.AreEqual(
                new[]
                {
                    "cpu_usage:20|g|@0|card:low",
                    "cpu_usage:30|g|@0|card:high",
                    "cpu_usage:40|g|@0",
                }, output);
        }

        [Test]
        public void AggregatesByCardinalityWithTags()
        {
            var handler = new BufferBuilderHandlerMock();
            var aggregator = new GaugeAggregator(MetricAggregatorParametersFactory.Create(handler.Object));

            // Add metrics with different combinations of cardinality and tags
            AddStatsMetric(aggregator, "memory", 100.0, Cardinality.Low, new[] { "host:server1" });
            AddStatsMetric(aggregator, "memory", 200.0, Cardinality.Low, new[] { "host:server1" }); // Same key - should replace
            AddStatsMetric(aggregator, "memory", 150.0, Cardinality.Low, new[] { "host:server2" }); // Different tag - different key
            AddStatsMetric(aggregator, "memory", 300.0, Cardinality.High, new[] { "host:server1" }); // Different cardinality - different key

            aggregator.TryFlush(force: true);

            var output = handler.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries).OrderBy(s => s).ToArray();

            Assert.AreEqual(
                new[]
                {
                    "memory:150|g|@0|#host:server2|card:low",
                    "memory:200|g|@0|#host:server1|card:low",
                    "memory:300|g|@0|#host:server1|card:high",
                }, output);
        }

        private static void AddStatsMetric(GaugeAggregator aggregator, string statName, double value, Cardinality? cardinality = null, string[] tags = null)
        {
            var statsMetric = new StatsMetric
            {
                MetricType = MetricType.Gauge,
                StatName = statName,
                NumericValue = value,
                Cardinality = cardinality,
                Tags = tags,
            };

            aggregator.OnNewValue(ref statsMetric);
        }

        private static void AddStatsMetric(GaugeAggregator aggregator, string statName, double value)
        {
            AddStatsMetric(aggregator, statName, value, null, null);
        }
    }
}
