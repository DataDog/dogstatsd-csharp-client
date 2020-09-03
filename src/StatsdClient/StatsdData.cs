using System;
using System.Collections.Generic;
using StatsdClient.Bufferize;
using StatsdClient.Transport;

namespace StatsdClient
{
    internal class StatsdData : IDisposable
    {
        private List<ITransport> _transport;
        private List<StatsBufferize> _statsBufferize;

        public StatsdData(
            MetricsSender metricsSender,
            List<StatsBufferize> statsBufferize,
            List<ITransport> transport,
            Telemetry telemetry)
        {
            MetricsSender = metricsSender;
            Telemetry = telemetry;
            _statsBufferize = statsBufferize;
            _transport = transport;
        }

        public MetricsSender MetricsSender { get; private set; }

        public Telemetry Telemetry { get; private set; }

        public void Dispose()
        {
            // _statsBufferize and _telemetry must be disposed before _statsSender to make
            // sure _statsSender does not received data when it is already disposed.

            Telemetry?.Dispose();
            Telemetry = null;

            foreach (var d in _statsBufferize)
            {
                d.Dispose();
            }
            _statsBufferize.Clear();
            // _statsBufferize?.Dispose();
            // _statsBufferize = null;

            foreach (var d in _transport)
            {
                d.Dispose();
            }
            _transport.Clear();

            // _transport?.Dispose();
            // _transport = null;

            MetricsSender = null;
        }
    }
}