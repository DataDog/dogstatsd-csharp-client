using System;
using StatsdClient.Bufferize;

namespace StatsdClient.Aggregator
{
    internal class MetricAggregatorParameters
    {
        public MetricAggregatorParameters(
            MetricSerializer serializer,
            BufferBuilder bufferBuilder,
            int maxUniqueStatsBeforeFlush,
            TimeSpan flushInterval)
        {
            Serializer = serializer;
            BufferBuilder = bufferBuilder;
            MaxUniqueStatsBeforeFlush = maxUniqueStatsBeforeFlush;
            FlushInterval = flushInterval;
        }

        public MetricSerializer Serializer { get; }

        public BufferBuilder BufferBuilder { get; }

        public int MaxUniqueStatsBeforeFlush { get; }

        public TimeSpan FlushInterval { get; }
    }
}
