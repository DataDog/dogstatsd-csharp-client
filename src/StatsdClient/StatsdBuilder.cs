using System;
using System.Net;
using Mono.Unix;
using StatsdClient.Bufferize;

namespace StatsdClient
{
    /// <summary>
    /// StatsdBuilder builds an instance of `Statsd` from StatsdConfig.
    /// </summary>
    class StatsdBuilder : IDisposable
    {
        public static readonly string UnixDomainSocketPrefix = "unix://";

        readonly IStatsBufferizeFactory _factory;

        StatsSender _statsSender;
        StatsBufferize _statsBufferize;


        public StatsdBuilder(IStatsBufferizeFactory factory)
        {
            _factory = factory;
        }

        public Statsd BuildStats(StatsdConfig config)
        {
            var statsdServerName = !string.IsNullOrEmpty(config.StatsdServerName)
                    ? config.StatsdServerName
                    : Environment.GetEnvironmentVariable(StatsdConfig.DD_AGENT_HOST_ENV_VAR);

            if (string.IsNullOrEmpty(statsdServerName))
            {
                throw new ArgumentNullException(
                    $"{nameof(config)}.{nameof(config.StatsdServerName)} and"
                    + $" {StatsdConfig.DD_AGENT_HOST_ENV_VAR} environment variable not set");
            }

            var metricsSender = CreateMetricsSender(config, statsdServerName);
            var statsD = new Statsd(metricsSender,
                                    new RandomGenerator(),
                                    new StopWatchFactory(),
                                    "",
                                    config.ConstantTags);
            statsD.TruncateIfTooLong = config.StatsdTruncateIfTooLong;
            return statsD;
        }

        IStatsdUDP CreateMetricsSender(StatsdConfig config, string statsdServerName)
        {
            int bufferCapacity;
            if (statsdServerName.StartsWith(UnixDomainSocketPrefix))
            {
                statsdServerName = statsdServerName.Substring(UnixDomainSocketPrefix.Length);
                var endPoint = new UnixEndPoint(statsdServerName);
                _statsSender = _factory.CreateUnixDomainSocketStatsSender(endPoint,
                                                                          config.Advanced.UDSBufferFullBlockDuration);
                bufferCapacity = config.StatsdMaxUnixDomainSocketPacketSize;
            }
            else
            {
                _statsSender = CreateUDPStatsSender(config, statsdServerName);
                bufferCapacity = config.StatsdMaxUDPPacketSize;
            }

            _statsBufferize = CreateStatsBufferize(_statsSender,
                                                   bufferCapacity,
                                                   config.Advanced);
            return _statsBufferize;
        }

        StatsBufferize CreateStatsBufferize(
            StatsSender statsSender,
            int bufferCapacity,
            AdvancedStatsConfig config)
        {
            var bufferHandler = new BufferBuilderHandler(new Telemetry(), statsSender);
            var bufferBuilder = new BufferBuilder(bufferHandler, bufferCapacity, "\n");

            var statsBufferize = _factory.CreateStatsBufferize(
                bufferBuilder,
                config.MaxMetricsInAsyncQueue,
                config.MaxBlockDuration,
                config.DurationBeforeSendingNotFullBuffer);

            return statsBufferize;
        }

        StatsSender CreateUDPStatsSender(StatsdConfig config, string statsdServerName)
        {
            var address = StatsdUDP.GetIpv4Address(statsdServerName);
            var port = GetPort(config);

            var endPoint = new IPEndPoint(address, port);
            return _factory.CreateUDPStatsSender(endPoint);
        }

        static int GetPort(StatsdConfig config)
        {
            if (config.StatsdPort != 0)
                return config.StatsdPort;

            var portString = Environment.GetEnvironmentVariable(StatsdConfig.DD_DOGSTATSD_PORT_ENV_VAR);
            if (!string.IsNullOrEmpty(portString))
            {
                if (Int32.TryParse(portString, out var port))
                    return port;
                throw new ArgumentException($"Environment Variable '{StatsdConfig.DD_DOGSTATSD_PORT_ENV_VAR}' bad format: {portString}");
            }

            return StatsdConfig.DefaultStatsdPort;
        }

        public void Dispose()
        {
            // _statsBufferize must be disposed before _statsSender to make
            // sure _statsBufferize does not send data to a disposed object.
            _statsBufferize?.Dispose();
            _statsSender?.Dispose();
            _statsBufferize = null;
            _statsSender = null;
        }
    }
}
