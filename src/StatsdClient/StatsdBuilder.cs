using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Mono.Unix;
using StatsdClient.Bufferize;

namespace StatsdClient
{
    /// <summary>
    /// StatsdBuilder builds an instance of `Statsd` from StatsdConfig.
    /// </summary>
    internal class StatsdBuilder
    {
        public static readonly string UnixDomainSocketPrefix = "unix://";
        private const string _entityIdInternalTagKey = "dd.internal.entity_id";

        private readonly IStatsBufferizeFactory _factory;

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
            var globalTags = GetGlobalTags(config);
            var telemetry = CreateTelemetry(config, globalTags, statsSenderData.Sender);
            var statsBufferize = CreateStatsBufferize(
                telemetry,
                statsSenderData.Sender,
                statsSenderData.BufferCapacity,
                config.Advanced);
            var metricsSender = new MetricsSender(
                statsBufferize,
                new RandomGenerator(),
                new StopWatchFactory(),
                string.Empty,
                config.ConstantTags,
                telemetry);
            metricsSender.TruncateIfTooLong = config.StatsdTruncateIfTooLong;
            return new StatsdData(metricsSender, statsBufferize, statsSender, telemetry);
        }

        private static int GetPort(StatsdConfig config)
        {
            if (config.StatsdPort != 0)
            {
                return config.StatsdPort;
            }

            var portString = Environment.GetEnvironmentVariable(StatsdConfig.DD_DOGSTATSD_PORT_ENV_VAR);
            if (!string.IsNullOrEmpty(portString))
            {
                if (int.TryParse(portString, out var port))
                {
                    return port;
                }

                throw new ArgumentException($"Environment Variable '{StatsdConfig.DD_DOGSTATSD_PORT_ENV_VAR}' bad format: {portString}");
            }

            return StatsdConfig.DefaultStatsdPort;
        }

        private string[] GetGlobalTags(StatsdConfig config)
        {
            var globalTags = new List<string>();

            if (config.ConstantTags != null)
            {
                globalTags.AddRange(config.ConstantTags);
            }

            string entityId = Environment.GetEnvironmentVariable(StatsdConfig.EntityIdEnvVar);
            if (!string.IsNullOrEmpty(entityId))
            {
                globalTags.Add($"{_entityIdInternalTagKey}:{entityId}");
            }

            return globalTags.ToArray();
        }

        private Telemetry CreateTelemetry(
            StatsdConfig config,
            string[] globalTags,
            IStatsSender statsSender)
        {
            var telemetryFlush = config.Advanced.TelemetryFlushInterval;

            if (telemetryFlush.HasValue)
            {
                var assembly = typeof(StatsdBuilder).GetTypeInfo().Assembly;
                var version = assembly.GetName().Version.ToString();

                return _factory.CreateTelemetry(version, telemetryFlush.Value, statsSender, globalTags);
            }

            // Telemetry is not enabled
            return new Telemetry();
        }

        private StatsSenderData CreateStatsSender(StatsdConfig config, string statsdServerName)
        {
            var statsSenderData = new StatsSenderData();

            if (statsdServerName.StartsWith(UnixDomainSocketPrefix))
            {
                statsdServerName = statsdServerName.Substring(UnixDomainSocketPrefix.Length);
                var endPoint = new UnixEndPoint(statsdServerName);
                statsSenderData.Sender = _factory.CreateUnixDomainSocketStatsSender(
                    endPoint,
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

        private StatsBufferize CreateStatsBufferize(
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

        private StatsSender CreateUDPStatsSender(StatsdConfig config, string statsdServerName)
        {
            var address = StatsdUDP.GetIpv4Address(statsdServerName);
            var port = GetPort(config);

            var endPoint = new IPEndPoint(address, port);
            return _factory.CreateUDPStatsSender(endPoint);
        }

        private class StatsSenderData
        {
            public StatsSender Sender { get; set; }

            public int BufferCapacity { get; set; }
        }
    }
}
