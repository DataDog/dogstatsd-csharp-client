using System;
using System.Collections.Generic;
using System.Net;
using Mono.Unix;

namespace StatsdClient
{
    class StatsdBuilder : IDisposable
    {
        static readonly string UnixDomainSocketPrefix = "unix://";

        // This field can be removed when Statsd can dispose Statsd.Udp.
        readonly List<IDisposable> _disposables = new List<IDisposable>();

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
            StatsSender statsSender;
            int bufferCapacity;
            if (statsdServerName.StartsWith(UnixDomainSocketPrefix))
            {
                statsdServerName = statsdServerName.Substring(UnixDomainSocketPrefix.Length);
                var endPoint = new UnixEndPoint(statsdServerName);
                statsSender = StatsSender.CreateUnixDomainSocketStatsSender(endPoint);
                bufferCapacity = config.StatsdMaxUnixDomainSocketPacketSize;
            }
            else
            {
                statsSender = CreateUDPStatsSender(config, statsdServerName);
                bufferCapacity = config.StatsdMaxUDPPacketSize;
            }

            _disposables.Add(statsSender);

            return CreateStatsBufferize(
                statsSender,
                bufferCapacity,
                config.Advanced);
        }

        StatsBufferize CreateStatsBufferize(
            StatsSender statsSender,
            int bufferCapacity,
            AdvancedStatsConfig config)
        {
            var bufferBuilder = new BufferBuilder(statsSender, bufferCapacity, "\n");

            var statsBufferize = new StatsBufferize(
                bufferBuilder,
                config.MaxMetricsInAsyncQueue,
                config.MaxBlockDuration,
                config.DurationBeforeSendingNotFullBuffer);

            _disposables.Add(statsBufferize);
            return statsBufferize;
        }

        static StatsSender CreateUDPStatsSender(StatsdConfig config, string statsdServerName)
        {
            var address = StatsdUDP.GetIpv4Address(statsdServerName);
            var port = GetPort(config);

            var endPoint = new IPEndPoint(address, port);
            return StatsSender.CreateUDPStatsSender(endPoint);
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
            foreach (var d in _disposables)
                d.Dispose();
            _disposables.Clear();
        }
    }
}
