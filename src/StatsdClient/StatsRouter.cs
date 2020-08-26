using System;
using StatsdClient.Bufferize;
using StatsdClient.Statistic;

namespace StatsdClient
{
    /// <summary>
    /// Route `Stats` instance.
    /// Route always to BufferBuilder.
    /// </summary>
    internal class StatsRouter
    {
        private readonly Serializers _serializers;
        private readonly BufferBuilder _bufferBuilder;

        public StatsRouter(
            Serializers serializers,
            BufferBuilder bufferBuilder)
        {
            _serializers = serializers;
            _bufferBuilder = bufferBuilder;
        }

        public void Route(Stats stats)
        {
            SerializedMetric serializedMetric = null;

            switch (stats.Kind)
            {
                case StatsKind.Event:
                    serializedMetric = this._serializers.EventSerializer.Serialize(ref stats.Event, stats.Tags);
                    break;
                case StatsKind.Metric:
                    serializedMetric = this._serializers.MetricSerializer.Serialize(ref stats.Metric, stats.Tags);
                    break;
                case StatsKind.ServiceCheck:
                    serializedMetric = this._serializers.ServiceCheckSerializer.Serialize(ref stats.ServiceCheck, stats.Tags);
                    break;
                default:
                    throw new ArgumentException($"{stats.Kind} is not supported");
            }

            if (serializedMetric != null)
            {
                if (!_bufferBuilder.Add(serializedMetric))
                {
                    throw new InvalidOperationException($"The metric size exceeds the buffer capacity: {serializedMetric.ToString()}");
                }
            }
        }

        public void TryFlush()
        {
            _bufferBuilder.HandleBufferAndReset();
        }
    }
}