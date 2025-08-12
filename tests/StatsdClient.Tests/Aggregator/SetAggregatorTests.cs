using System;
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

            var output = handler.Value;
            // Should have separate sets for each cardinality
            // Low cardinality: user1, user2 (2 unique values)
            // High cardinality: user1 (1 unique value)
            // Null cardinality: user3 (1 unique value)
            var lowCardinalityLines = 0;
            var highCardinalityLines = 0;
            var nullCardinalityLines = 0;

            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("unique_users:") && line.Contains("|s"))
                {
                    if (line.Contains("card:low"))
                    {
                        lowCardinalityLines++;
                    }
                    else if (line.Contains("card:high"))
                    {
                        highCardinalityLines++;
                    }
                    else if (!line.Contains("card:"))
                    {
                        nullCardinalityLines++;
                    }
                }
            }

            Assert.AreEqual(2, lowCardinalityLines, "Expected 2 unique values in Low cardinality set");
            Assert.AreEqual(1, highCardinalityLines, "Expected 1 unique value in High cardinality set");
            Assert.AreEqual(1, nullCardinalityLines, "Expected 1 unique value in null cardinality set");
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

            var output = handler.Value;

            var usLowCount = 0;
            var euLowCount = 0;
            var usHighCount = 0;

            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("session_ids:") && line.Contains("|s"))
                {
                    if (line.Contains("card:low") && line.Contains("region:us"))
                    {
                        usLowCount++;
                    }
                    else if (line.Contains("card:low") && line.Contains("region:eu"))
                    {
                        euLowCount++;
                    }
                    else if (line.Contains("card:high") && line.Contains("region:us"))
                    {
                        usHighCount++;
                    }
                }
            }

            Assert.AreEqual(2, usLowCount, "Expected 2 unique values in Low cardinality, US region set");
            Assert.AreEqual(1, euLowCount, "Expected 1 unique value in Low cardinality, EU region set");
            Assert.AreEqual(1, usHighCount, "Expected 1 unique value in High cardinality, US region set");
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
