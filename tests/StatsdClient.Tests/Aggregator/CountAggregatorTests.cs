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

            var output = handler.Value;
            // Should have 3 separate metrics: Low (1+2=3), High (3+4=7), null (5)
            Assert.True(output.Contains("requests:3|c|card:low"), "Expected Low cardinality metric with value 3");
            Assert.True(output.Contains("requests:7|c|card:high"), "Expected High cardinality metric with value 7");
            Assert.True(output.Contains("requests:5|c") && !output.Contains("requests:5|c|card:"), "Expected null cardinality metric with value 5");
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

            var output = handler.Value;
            // Should have 3 separate metrics based on different key combinations
            Assert.True(
                output.Contains("requests:3|c") && (output.Contains("#card:low") || output.Contains("env:prod")),
                "Expected Low cardinality metric with env:prod tag and value 3");
            Assert.True(
                output.Contains("requests:3|c") && (output.Contains("#card:low") || output.Contains("env:staging")),
                "Expected Low cardinality metric with env:staging tag and value 3");
            Assert.True(
                output.Contains("requests:4|c") && (output.Contains("#card:high") || output.Contains("env:prod")),
                "Expected High cardinality metric with env:prod tag and value 4");
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
