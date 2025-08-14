using System;
using System.Linq;
using NUnit.Framework;
using StatsdClient.Aggregator;
using StatsdClient.Statistic;
using Tests.Utils;

namespace StatsdClient.Tests.Aggregator
{
    [TestFixture]
    public class SetAggregatorTests
    {
        [Test]
        public void OnNewValue()
        {
            var handler = new BufferBuilderHandlerMock();
            var aggregator = new SetAggregator(MetricAggregatorParametersFactory.Create(handler.Object), null, Tools.ExceptionHandler);
            AddStatsMetric(aggregator, "s1", "1");
            AddStatsMetric(aggregator, "s1", "2");
            AddStatsMetric(aggregator, "s1", "2");
            AddStatsMetric(aggregator, "s1", "2");
            AddStatsMetric(aggregator, "s2", "3");
            aggregator.TryFlush(force: true);
            Assert.AreEqual("s1:1|s|@0\ns1:2|s|@0\ns2:3|s|@0\n", handler.Value);

            AddStatsMetric(aggregator, "s1", "4");
            aggregator.TryFlush(force: true);
            Assert.AreEqual("s1:4|s|@0\n", handler.Value);
        }

        [Test]
        public void Pool()
        {
            var handler = new BufferBuilderHandlerMock();
            var parameters = MetricAggregatorParametersFactory.Create(handler.Object, TimeSpan.FromMinutes(1), 1);
            var aggregator = new SetAggregator(parameters, null, Tools.ExceptionHandler);

            // Check StatsMetricSet instance go back to the pool
            for (int i = 0; i < 10; ++i)
            {
                AddStatsMetric(aggregator, "s1", i.ToString());
                Assert.AreEqual($"s1:{i}|s|@0\n", handler.Value);
            }
        }

        [Test]
        public void AggregatesByCardinality()
        {
            var handler = new BufferBuilderHandlerMock();
            var aggregator = new SetAggregator(MetricAggregatorParametersFactory.Create(handler.Object), null, Tools.ExceptionHandler);

            // Add metrics with same name but different cardinalities
            // Set aggregation keeps unique values per unique key
            AddStatsMetric(aggregator, "unique_users", "user1", Cardinality.Low);
            AddStatsMetric(aggregator, "unique_users", "user2", Cardinality.Low); // Same cardinality - should be in same set
            AddStatsMetric(aggregator, "unique_users", "user1", Cardinality.Low); // Duplicate value in same set - should dedupe
            AddStatsMetric(aggregator, "unique_users", "user1", Cardinality.High); // Different cardinality - different set
            AddStatsMetric(aggregator, "unique_users", "user3", null); // Different cardinality (null) - different set

            aggregator.TryFlush(force: true);

            var output = handler.Value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(s => s).ToArray();

            Assert.AreEqual(
                new[]
                {
                    "unique_users:user1|s|@0|card:high",
                    "unique_users:user1|s|@0|card:low",
                    "unique_users:user2|s|@0|card:low",
                    "unique_users:user3|s|@0",
                }, output);
        }

        [Test]
        public void AggregatesByCardinalityWithTags()
        {
            var handler = new BufferBuilderHandlerMock();
            var aggregator = new SetAggregator(MetricAggregatorParametersFactory.Create(handler.Object), null, Tools.ExceptionHandler);

            // Add metrics with different combinations of cardinality and tags
            AddStatsMetric(aggregator, "session_ids", "session1", Cardinality.Low, new[] { "region:us" });
            AddStatsMetric(aggregator, "session_ids", "session2", Cardinality.Low, new[] { "region:us" }); // Same key - same set
            AddStatsMetric(aggregator, "session_ids", "session3", Cardinality.Low, new[] { "region:eu" }); // Different tag - different key
            AddStatsMetric(aggregator, "session_ids", "session4", Cardinality.High, new[] { "region:us" }); // Different cardinality - different key

            aggregator.TryFlush(force: true);
            var output = handler.Value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(s => s).ToArray();

            Assert.AreEqual(
                new[]
                {
                    "session_ids:session1|s|@0|#region:us|card:low",
                    "session_ids:session2|s|@0|#region:us|card:low",
                    "session_ids:session3|s|@0|#region:eu|card:low",
                    "session_ids:session4|s|@0|#region:us|card:high",
                }, output);
        }

        private static void AddStatsMetric(SetAggregator aggregator, string statName, string value, Cardinality? cardinality = null, string[] tags = null)
        {
            var statsMetric = new StatsMetric
            {
                MetricType = MetricType.Set,
                StatName = statName,
                StringValue = value,
                Cardinality = cardinality,
                Tags = tags,
            };

            aggregator.OnNewValue(ref statsMetric);
        }

        private static void AddStatsMetric(SetAggregator aggregator, string statName, string value)
        {
            AddStatsMetric(aggregator, statName, value, null, null);
        }
    }
}
