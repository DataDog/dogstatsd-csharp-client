using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;
using StatsdClient.Aggregator;
using StatsdClient.Bufferize;
using StatsdClient.Statistic;
using StatsdClient.Utils;
using Tests.Utils;

namespace Tests
{
    [TestFixture]
    public class StatsRouterTests
    {
        [Test]
        public void Routing()
        {
            var handler = new BufferBuilderHandlerMock();
            var bufferBuilder = new BufferBuilder(handler, 1024, "\n");
            var serializers = new Serializers
            {
                MetricSerializer = new MetricSerializer(new SerializerHelper(null), string.Empty),
            };

            // a few metrics

            var statsMetricWithoutTimestamp = new StatsMetric
            {
                MetricType = MetricType.Count,
                StatName = "count.name",
                SampleRate = 1.0,
                NumericValue = 40,
                Tags = new[] { "tag1:true", "tag2" },
            };

            var statsWithoutTimestamp = new Stats
            {
                Kind = StatsKind.Metric,
                Metric = statsMetricWithoutTimestamp,
            };

            var dto = new DateTimeOffset(2013, 05, 01, 18, 30, 00, new TimeSpan(0, 0, 0));
            var statsMetricWithTimestamp = new StatsMetric
            {
                MetricType = MetricType.Count,
                StatName = "count.name",
                SampleRate = 1.0,
                NumericValue = 40,
                Tags = new[] { "tag1:true", "tag2" },
                Timestamp = DateTimeOffsetHelper.ToUnixTimeSeconds(dto),
            };

            var statsWithTimestamp = new Stats
            {
                Kind = StatsKind.Metric,
                Metric = statsMetricWithTimestamp,
            };

            // without client side aggregation

            var statsRouter = new StatsRouter(serializers, bufferBuilder, null);

            // no client side aggregation without timestamp,
            // should be immediately written on the serializer
            statsRouter.Route(statsWithoutTimestamp);
            statsRouter.Route(statsWithoutTimestamp);
            statsRouter.Flush();
            Assert.AreEqual("count.name:40|c|#tag1:true,tag2\ncount.name:40|c|#tag1:true,tag2\n", handler.BufferToString());
            handler.Reset();

            // no client side aggregation with timestamp,
            // should be immediately written on the serializer
            statsRouter.Route(statsWithTimestamp);
            statsRouter.Route(statsWithTimestamp);
            statsRouter.Flush();
            Assert.AreEqual("count.name:40|c|#tag1:true,tag2|T1367433000\ncount.name:40|c|#tag1:true,tag2|T1367433000\n", handler.BufferToString());
            handler.Reset();

            // with client side aggregation

            var parameters = new MetricAggregatorParameters(
                serializers.MetricSerializer,
                bufferBuilder,
                1000,
                TimeSpan.FromSeconds(100),
                null);

            var optionalAggregators = new Aggregators
            {
                OptionalCount = new CountAggregator(parameters),
            };

            statsRouter = new StatsRouter(serializers, bufferBuilder, optionalAggregators);

            // client side aggregation, no timestamp,
            // should be aggregated in only one count
            statsRouter.Route(statsWithoutTimestamp);
            statsRouter.Route(statsWithoutTimestamp);
            statsRouter.Route(statsWithoutTimestamp);
            statsRouter.Flush();
            Assert.AreEqual("count.name:120|c|#tag1:true,tag2\n", handler.BufferToString());
            handler.Reset();

            // client side aggregation, with timestamp,
            // should be immediately written on the serializer
            statsRouter.Route(statsWithTimestamp);
            statsRouter.Route(statsWithTimestamp);
            statsRouter.Route(statsWithTimestamp);
            statsRouter.Flush();
            Assert.AreEqual("count.name:40|c|#tag1:true,tag2|T1367433000\ncount.name:40|c|#tag1:true,tag2|T1367433000\ncount.name:40|c|#tag1:true,tag2|T1367433000\n", handler.BufferToString());
            handler.Reset();
        }
    }
}
