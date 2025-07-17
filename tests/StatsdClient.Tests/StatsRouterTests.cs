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
        private BufferBuilderHandlerMock _handler;
        private BufferBuilder _bufferBuilder;
        private Serializers _serializers;
        private Stats _statsWithoutTimestamp;
        private Stats _statsWithTimestamp;
        private Aggregators _optionalAggregators;

        [SetUp]
        public void Init()
        {
            _handler = new BufferBuilderHandlerMock();
            _bufferBuilder = new BufferBuilder(_handler, 1024, "\n", null);
            _serializers = new Serializers
            {
                MetricSerializer = new MetricSerializer(new SerializerHelper(null, null), string.Empty),
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

            _statsWithoutTimestamp = new Stats
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
                Timestamp = dto.ToUnixTimeSeconds(),
            };

            _statsWithTimestamp = new Stats
            {
                Kind = StatsKind.Metric,
                Metric = statsMetricWithTimestamp,
            };

            var parameters = new MetricAggregatorParameters(
                _serializers.MetricSerializer,
                _bufferBuilder,
                1000,
                TimeSpan.FromSeconds(100),
                null);

            _optionalAggregators = new Aggregators
            {
                OptionalCount = new CountAggregator(parameters),
            };
        }

        [Test]
        public void StatsRouterWithoutAggWithoutTS()
        {
            // without client side aggregation
            var statsRouter = new StatsRouter(_serializers, _bufferBuilder, null);

            // no client side aggregation without timestamp,
            // should be immediately written on the serializer
            statsRouter.Route(_statsWithoutTimestamp);
            statsRouter.Route(_statsWithoutTimestamp);
            statsRouter.Flush();
            Assert.AreEqual("count.name:40|c|#tag1:true,tag2\ncount.name:40|c|#tag1:true,tag2\n", _handler.BufferToString());
            _handler.Reset();
        }

        [Test]
        public void StatsRouterWithoutAggWithTS()
        {
            // without client side aggregation
            var statsRouter = new StatsRouter(_serializers, _bufferBuilder, null);

            // no client side aggregation with timestamp,
            // should be immediately written on the serializer
            statsRouter.Route(_statsWithTimestamp);
            statsRouter.Route(_statsWithTimestamp);
            statsRouter.Flush();
            Assert.AreEqual("count.name:40|c|#tag1:true,tag2|T1367433000\ncount.name:40|c|#tag1:true,tag2|T1367433000\n", _handler.BufferToString());
            _handler.Reset();
        }

        [Test]
        public void StatsRouterWithAggWithoutTS()
        {
            var statsRouter = new StatsRouter(_serializers, _bufferBuilder, _optionalAggregators);

            // client side aggregation, no timestamp,
            // should be aggregated in only one count
            statsRouter.Route(_statsWithoutTimestamp);
            statsRouter.Route(_statsWithoutTimestamp);
            statsRouter.Route(_statsWithoutTimestamp);
            statsRouter.Flush();
            Assert.AreEqual("count.name:120|c|#tag1:true,tag2\n", _handler.BufferToString());
            _handler.Reset();
        }

        [Test]
        public void WithAggWithoutTS()
        {
            var statsRouter = new StatsRouter(_serializers, _bufferBuilder, _optionalAggregators);

            // client side aggregation, with timestamp,
            // should be immediately written on the serializer
            statsRouter.Route(_statsWithTimestamp);
            statsRouter.Route(_statsWithTimestamp);
            statsRouter.Route(_statsWithTimestamp);
            statsRouter.Flush();
            Assert.AreEqual("count.name:40|c|#tag1:true,tag2|T1367433000\ncount.name:40|c|#tag1:true,tag2|T1367433000\ncount.name:40|c|#tag1:true,tag2|T1367433000\n", _handler.BufferToString());
            _handler.Reset();
        }
    }
}
