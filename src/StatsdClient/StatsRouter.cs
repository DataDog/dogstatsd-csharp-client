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
        private SerializedMetric _serializedMetric = new SerializedMetric();

        public StatsRouter(
            Serializers serializers,
            BufferBuilder bufferBuilder)
        {
            _serializers = serializers;
            _bufferBuilder = bufferBuilder;
        }

        public void Route(Stats stats)
        {
            switch (stats.Kind)
            {
                case StatsKind.Event:
                    this._serializers.EventSerializer.SerializeTo(ref stats.Event, stats.Tags, _serializedMetric);
                    break;
                case StatsKind.Metric:
                    this._serializers.MetricSerializer.SerializeTo(ref stats.Metric, stats.Tags, _serializedMetric);
                    break;
                case StatsKind.ServiceCheck:
                    this._serializers.ServiceCheckSerializer.SerializeTo(ref stats.ServiceCheck, stats.Tags, _serializedMetric);
                    break;
                default:
                    throw new ArgumentException($"{stats.Kind} is not supported");
            }

            if (!_bufferBuilder.Add(_serializedMetric))
            {
                throw new InvalidOperationException($"The metric size exceeds the buffer capacity: {_serializedMetric.ToString()}");
            }
        }

        public void TryFlush()
        {
            _bufferBuilder.HandleBufferAndReset();
        }
    }
}