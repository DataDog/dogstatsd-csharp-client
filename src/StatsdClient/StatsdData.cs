using System;
using StatsdClient.Bufferize;

namespace StatsdClient
{
    internal class StatsdData : IDisposable
    {
        private StatsSender _statsSender;
        private StatsBufferize _statsBufferize;

        public StatsdData(Statsd statsd,
                          StatsBufferize statsBufferize,
                          StatsSender statsSender,
                          Telemetry telemetry)
        {
            Statsd = statsd;
            Telemetry = telemetry;
            _statsBufferize = statsBufferize;
            _statsSender = statsSender;
        }

        public Statsd Statsd { get; private set; }
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

            Statsd = null;
        }
    }
}