using System;
using StatsdClient.Aggregator;
using StatsdClient.Bufferize;
using Tests.Utils;

namespace StatsdClient.Tests.Aggregator
{
    /// <summary>
    /// Create an instance of `MetricAggregatorParameters` for the tests.
    /// </summary>
    internal static class MetricAggregatorParametersFactory
    {
        public static MetricAggregatorParameters Create(IBufferBuilderHandler handler)
        {
            return Create(handler, TimeSpan.FromMinutes(1), 1000);
        }

        public static MetricAggregatorParameters Create(
            IBufferBuilderHandler handler,
            TimeSpan flushInterval,
            int maxUniqueStatsBeforeFlush)
        {
            var serializer = new MetricSerializer(new SerializerHelper(null, null), string.Empty);
            var bufferBuilder = new BufferBuilder(handler, bufferCapacity: 1000, "\n", Tools.ExceptionHandler);

            return new MetricAggregatorParameters(
                serializer,
                bufferBuilder,
                maxUniqueStatsBeforeFlush,
                flushInterval,
                null);
        }
    }
}
