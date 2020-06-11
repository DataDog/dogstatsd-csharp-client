using System;
using StatsdClient.Bufferize;

namespace StatsdClient
{
    internal class StatsdData : IDisposable
    {
        private IStatsSender _statsSender;
        private StatsBufferize _statsBufferize;

        public StatsdData(
            MetricsSender metricsSender,
            StatsBufferize statsBufferize,
            IStatsSender statsSender,
            Telemetry telemetry)
        {
            MetricsSender = metricsSender;
            Telemetry = telemetry;
            _statsBufferize = statsBufferize;
            _statsSender = statsSender;
        }

        public MetricsSender MetricsSender { get; private set; }

        public Telemetry Telemetry { get; private set; }

        public void Dispose()
        {
            // _statsBufferize and _telemetry must be disposed before _statsSender to make
            // sure _statsSender does not received data when it is already disposed.

            Telemetry?.Dispose();
            Telemetry = null;

            _statsBufferize?.Dispose();
            _statsBufferize = null;

            _statsSender?.Dispose();
            _statsSender = null;

            MetricsSender = null;
        }
    }
}