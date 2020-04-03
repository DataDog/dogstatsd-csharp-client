using System;
using StatsdClient.Bufferize;

namespace StatsdClient
{
    internal class StatsdData : IDisposable
    {
        private StatsSender _statsSender;
        private StatsBufferize _statsBufferize;
        private Telemetry _telemetry;

        public StatsdData(Statsd statsd,
                          StatsBufferize statsBufferize,
                          StatsSender statsSender,
                          Telemetry telemetry)
        {
            Statsd = statsd;
            _statsBufferize = statsBufferize;
            _statsSender = statsSender;
            _telemetry = telemetry;
        }

        public Statsd Statsd { get; }

        public void Dispose()
        {
            // _statsBufferize and _telemetry must be disposed before _statsSender to make
            // sure _statsSender does not received data when it is already disposed.

            _telemetry?.Dispose();
            _telemetry = null;

            _statsBufferize?.Dispose();
            _statsBufferize = null;

            _statsSender?.Dispose();
            _statsSender = null;
        }
    }
}