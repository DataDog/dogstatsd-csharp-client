using System;
using StatsdClient.Bufferize;
using StatsdClient.Transport;

namespace StatsdClient
{
    internal class StatsdData : IDisposable
    {
        private ITransport _transport;
        private IStatsSender _statsSender;

        public StatsdData(
            MetricsSender metricsSender,
            IStatsSender statsSender,
            ITransport transport,
            Telemetry telemetry)
        {
            MetricsSender = metricsSender;
            Telemetry = telemetry;
            _statsSender = statsSender;
            _transport = transport;
        }

        public MetricsSender MetricsSender { get; private set; }

        public Telemetry Telemetry { get; private set; }

        public void Flush(bool flushTelemetry)
        {
            _statsSender?.Flush();
            if (flushTelemetry)
            {
                Telemetry.Flush();
            }
        }

        public void Dispose()
        {
            // _statsSender and _telemetry must be disposed before _statsSender to make
            // sure _statsSender does not receive data when it is already disposed.

            Telemetry?.Dispose();
            Telemetry = null;

            _statsSender?.Dispose();
            _statsSender = null;

            _transport?.Dispose();
            _transport = null;

            MetricsSender = null;
        }
    }
}
