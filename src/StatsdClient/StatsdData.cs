using System;
using System.Threading.Tasks;
using StatsdClient.Bufferize;
using StatsdClient.Transport;

namespace StatsdClient
{
    internal class StatsdData : IDisposable
    {
        private ITransport _transport;
        private StatsBufferize _statsBufferSize;

        public StatsdData(
            MetricsSender metricsSender,
            StatsBufferize statsBufferize,
            ITransport transport,
            Telemetry telemetry)
        {
            MetricsSender = metricsSender;
            Telemetry = telemetry;
            _statsBufferSize = statsBufferize;
            _transport = transport;
        }

        public MetricsSender MetricsSender { get; private set; }

        public Telemetry Telemetry { get; private set; }

        public void Flush(bool flushTelemetry)
        {
            _statsBufferSize?.Flush();
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

            _statsBufferSize?.Dispose();
            _statsBufferSize = null;

            _transport?.Dispose();
            _transport = null;

            MetricsSender = null;
        }

        public async Task DisposeAsync()
        {
            // _statsBufferize and _telemetry must be disposed before _statsSender to make
            // sure _statsSender does not receive data when it is already disposed.

            Telemetry?.Dispose();
            Telemetry = null;

            var statsBufferSize = _statsBufferSize;
            if (statsBufferSize != null)
            {
                await _statsBufferSize.DisposeAsync().ConfigureAwait(false);
                _statsBufferSize = null;
            }

            _transport?.Dispose();
            _transport = null;

            MetricsSender = null;
        }
    }
}
