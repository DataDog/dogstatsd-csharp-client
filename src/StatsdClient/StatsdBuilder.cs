using System;
using System.Net;
using System.Reflection;
using Mono.Unix;
using StatsdClient.Bufferize;

namespace StatsdClient
{
    /// <summary>
    /// StatsdBuilder builds an instance of `Statsd` from StatsdConfig.
    /// </summary>
    class StatsdBuilder
    {
        public static readonly string UnixDomainSocketPrefix = "unix://";

        readonly IStatsBufferizeFactory _factory;

        public StatsdBuilder(IStatsBufferizeFactory factory)
        {
            _factory = factory;
        }

        public StatsdData BuildStatsData(StatsdConfig config)
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

            var statsSenderData = CreateStatsSender(config, statsdServerName);
            var statsSender = statsSenderData.Sender;
            var telemetry = CreateTelemetry(config, statsSenderData.Sender);
            var statsBufferize = CreateStatsBufferize(telemetry,
                                                   statsSenderData.Sender,
                                                   statsSenderData.BufferCapacity,
                                                   config.Advanced);
            var statsD = new Statsd(statsBufferize,
                                    new RandomGenerator(),
                                    new StopWatchFactory(),
                                    "",
                                    config.ConstantTags,
                                    telemetry);
            statsD.TruncateIfTooLong = config.StatsdTruncateIfTooLong;
            return new StatsdData(statsD, statsBufferize, statsSender, telemetry);
        }

        Telemetry CreateTelemetry(StatsdConfig config, IStatsSender statsSender)
        {
            var telemetryFlush = config.Advanced.TelemetryFlushInterval;

            if (telemetryFlush.HasValue)
            {
                var assembly = typeof(StatsdBuilder).GetTypeInfo().Assembly;
                var version = assembly.GetName().Version.ToString();

                return new Telemetry(version, telemetryFlush.Value, statsSender);
            }

            // Telemetry is not enabled
            return new Telemetry();
        }

        class StatsSenderData
        {
            public StatsSender Sender { get; set; }
            public int BufferCapacity { get; set; }
        }

        StatsSenderData CreateStatsSender(StatsdConfig config, string statsdServerName)
        {
            var statsSenderData = new StatsSenderData();

            if (statsdServerName.StartsWith(UnixDomainSocketPrefix))
            {
                statsdServerName = statsdServerName.Substring(UnixDomainSocketPrefix.Length);
                var endPoint = new UnixEndPoint(statsdServerName);
                statsSenderData.Sender = _factory.CreateUnixDomainSocketStatsSender(endPoint,
                                                                          config.Advanced.UDSBufferFullBlockDuration);
                statsSenderData.BufferCapacity = config.StatsdMaxUnixDomainSocketPacketSize;
            }
            else
            {
                statsSenderData.Sender = CreateUDPStatsSender(config, statsdServerName);
                statsSenderData.BufferCapacity = config.StatsdMaxUDPPacketSize;
            }

            return statsSenderData;
        }

        StatsBufferize CreateStatsBufferize(
            Telemetry telemetry,
            StatsSender statsSender,
            int bufferCapacity,
            AdvancedStatsConfig config)
        {
            var bufferHandler = new BufferBuilderHandler(telemetry, statsSender);
            var bufferBuilder = new BufferBuilder(bufferHandler, bufferCapacity, "\n");

            var statsBufferize = _factory.CreateStatsBufferize(
                telemetry,
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
    }
}
