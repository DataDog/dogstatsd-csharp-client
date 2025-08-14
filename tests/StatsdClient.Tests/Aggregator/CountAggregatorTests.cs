using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using StatsdClient.Aggregator;
using StatsdClient.Statistic;
using Tests.Utils;

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
            Assert.AreEqual("s1:3|c\ns2:2|c\n", handler.Value);

            AddStatsMetric(aggregator, "s3", 1);
            aggregator.TryFlush(force: true);
            Assert.AreEqual("s3:1|c\n", handler.Value);
        }

        [Test]
        public void SampleRate()
        {
            var config = new StatsdConfig
            {
                StatsdServerName = "127.0.0.1",
                StatsdPort = 1234,
                OriginDetection = false,
            };
            config.ClientSideAggregation = new ClientSideAggregationConfig { };

            using (var server = new SocketServer(config))
            {
                using (var service = new DogStatsdService())
                {
                    service.Configure(config);
                    for (int i = 0; i < 500; i++)
                    {
                        service.Increment("test1", 1, 0.5);
                        service.Increment("test1", 1, 0.3);
                        service.Increment("test1", 1, 0.8);
                    }
                }

                var metric = server.Stop().Single();
                var match = Regex.Match(metric, @"^test1:(.*)\|c$");
                Assert.True(match.Success);
                var metricValue = double.Parse(match.Groups[1].Value);

                Assert.AreEqual(500.0 * 3, metricValue, delta: 100);
            }
        }

        [Test]
        public void AggregatesByCardinality()
        {
            var handler = new BufferBuilderHandlerMock();
            var aggregator = new CountAggregator(MetricAggregatorParametersFactory.Create(handler.Object));

            // Add metrics with same name/tags but different cardinalities
            AddStatsMetric(aggregator, "requests", 1, Cardinality.Low);
            AddStatsMetric(aggregator, "requests", 2, Cardinality.Low); // Should aggregate with above
            AddStatsMetric(aggregator, "requests", 3, Cardinality.High); // Should NOT aggregate with above
            AddStatsMetric(aggregator, "requests", 4, Cardinality.High); // Should aggregate with previous High
            AddStatsMetric(aggregator, "requests", 5, null); // Should NOT aggregate with any above

            aggregator.TryFlush(force: true);

            var output = handler.Value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(s => s).ToArray();

            Assert.AreEqual(
                new[]
                {
                    "requests:3|c|card:low",
                    "requests:5|c",
                    "requests:7|c|card:high",
                }, output);
        }

        [Test]
        public void AggregatesByCardinalityWithTags()
        {
            var handler = new BufferBuilderHandlerMock();
            var aggregator = new CountAggregator(MetricAggregatorParametersFactory.Create(handler.Object));

            // Add metrics with same name/cardinality but different tags
            AddStatsMetric(aggregator, "requests", 1, Cardinality.Low, new[] { "env:prod" });
            AddStatsMetric(aggregator, "requests", 2, Cardinality.Low, new[] { "env:prod" }); // Should aggregate
            AddStatsMetric(aggregator, "requests", 3, Cardinality.Low, new[] { "env:staging" }); // Different tags - should NOT aggregate
            AddStatsMetric(aggregator, "requests", 4, Cardinality.High, new[] { "env:prod" }); // Different cardinality - should NOT aggregate

            aggregator.TryFlush(force: true);

            var output = handler.Value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(s => s).ToArray();

            Assert.AreEqual(
                new[]
                {
                    "requests:3|c|#env:prod|card:low",
                    "requests:3|c|#env:staging|card:low",
                    "requests:4|c|#env:prod|card:high",
                }, output);
        }

        private static void AddStatsMetric(CountAggregator aggregator, string statName, double value, Cardinality? cardinality = null, string[] tags = null)
        {
            var statsMetric = new StatsMetric
            {
                MetricType = MetricType.Count,
                StatName = statName,
                NumericValue = value,
                SampleRate = 1,
                Cardinality = cardinality,
                Tags = tags,
            };

            aggregator.OnNewValue(ref statsMetric);
        }

        private static void AddStatsMetric(CountAggregator aggregator, string statName, double value)
        {
            AddStatsMetric(aggregator, statName, value, null, null);
        }
    }
}
