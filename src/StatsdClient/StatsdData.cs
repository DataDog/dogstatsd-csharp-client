using System;
using StatsdClient.Bufferize;
using StatsdClient.Transport;

namespace StatsdClient
{
    internal class StatsdData : IDisposable
    {
        private ITransport _transport;
        private StatsBufferize _statsBufferize;

        public StatsdData(
            MetricsSender metricsSender,
            StatsBufferize statsBufferize,
            ITransport transport,
            Telemetry telemetry)
        {
            MetricsSender = metricsSender;
            Telemetry = telemetry;
            _statsBufferize = statsBufferize;
            _transport = transport;
        }

        public MetricsSender MetricsSender { get; private set; }

        public Telemetry Telemetry { get; private set; }

        public void Flush(bool flushTelemetry)
        {
            _statsBufferize?.Flush();
            if (flushTelemetry)
            {
                Telemetry.Flush();
            }
        }

        public void Dispose()
        {
            // _statsBufferize and _telemetry must be disposed before _statsSender to make
            // sure _statsSender does not receive data when it is already disposed.

            Telemetry?.Dispose();
            Telemetry = null;

            _statsBufferize?.Dispose();
            _statsBufferize = null;

            _transport?.Dispose();
            _transport = null;

            MetricsSender = null;
        }
    }
}
