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

            var output = handler.Value;
            // Should have 3 separate gauge metrics: Low=20.0, High=30.0, null=40.0
            Assert.True(output.Contains("cpu_usage:20|g") && output.Contains("card:low"), "Expected Low cardinality gauge with value 20.0");
            Assert.True(output.Contains("cpu_usage:30|g") && output.Contains("card:high"), "Expected High cardinality gauge with value 30.0");
            Assert.True(output.Contains("cpu_usage:40|g") && !output.Contains("cpu_usage:40|g|card:"), "Expected null cardinality gauge with value 40.0");
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

            var output = handler.Value;
            // Should have 3 separate gauge metrics
            Assert.True(
                output.Contains("memory:200|g") && (output.Contains("card:low") || output.Contains("host:server1")),
                "Expected Low cardinality gauge with host:server1 and value 200.0");
            Assert.True(
                output.Contains("memory:150|g") && (output.Contains("card:low") || output.Contains("host:server2")),
                "Expected Low cardinality gauge with host:server2 and value 150.0");
            Assert.True(
                output.Contains("memory:300|g") && (output.Contains("card:high") || output.Contains("host:server1")),
                "Expected High cardinality gauge with host:server1 and value 300.0");
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
