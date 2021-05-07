using System;
using System.Threading;
using Moq;
using NUnit.Framework;
using StatsdClient.Aggregator;
using StatsdClient.Bufferize;
using StatsdClient.Statistic;

namespace StatsdClient.Tests.Aggregator
{
    [TestFixture]
    public class AggregatorFlusherTests
    {
        [Test]
        public void TryFlushMaxUniqueStatsBeforeFlush()
        {
            var handler = new Mock<IBufferBuilderHandler>();
            var parameters = MetricAggregatorParametersFactory.Create(
                handler.Object,
                TimeSpan.FromMinutes(1),
                maxUniqueStatsBeforeFlush: 3);

            var aggregator = new AggregatorFlusher<StatsMetric>(parameters, MetricType.Count, (a, v) => a.FlushStatsMetric(v));
            Add(aggregator, "key1", new StatsMetric { });
            Add(aggregator, "key2", new StatsMetric { });
            Flush(aggregator);

            handler.Verify(h => h.Handle(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Never());

            Add(aggregator, "key3", new StatsMetric { });
            Flush(aggregator);
            handler.Verify(h => h.Handle(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Once());

            Add(aggregator, "key4", new StatsMetric { });
            Flush(aggregator);
            handler.Verify(h => h.Handle(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Once());

            Flush(aggregator, force: true);
            handler.Verify(h => h.Handle(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public void TryFlushFlushInterval()
        {
            var handler = new Mock<IBufferBuilderHandler>();
            var parameters = MetricAggregatorParametersFactory.Create(
                handler.Object,
                TimeSpan.FromMilliseconds(500),
                maxUniqueStatsBeforeFlush: 100);

            var aggregator = new AggregatorFlusher<StatsMetric>(parameters, MetricType.Count, (a, v) => a.FlushStatsMetric(v));
            Add(aggregator, "key", new StatsMetric { });
            Flush(aggregator);
            handler.Verify(h => h.Handle(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Never());

            Thread.Sleep(TimeSpan.FromSeconds(1));
            Flush(aggregator);
            handler.Verify(h => h.Handle(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Once());
        }

        private static void Add(AggregatorFlusher<StatsMetric> aggregator, string key, StatsMetric value)
        {
            var statsKey = new MetricStatsKey(key, null);
            aggregator.Add(ref statsKey, value);
        }

        private static void Flush(AggregatorFlusher<StatsMetric> aggregator, bool force = false)
        {
            aggregator.TryFlush(force);
        }
    }
}