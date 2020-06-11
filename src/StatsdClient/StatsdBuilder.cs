using System;
using System.Collections.Generic;
using System.Linq;
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

            var statsSenderData = CreateTransport(config, statsdServerName);
            var transport = statsSenderData.Sender;
            var globalTags = GetGlobalTags(config);
            var telemetry = CreateTelemetry(config, globalTags, statsSenderData.Sender);
            var statsBufferize = CreateStatsBufferize(
                telemetry,
                statsSenderData.Sender,
                statsSenderData.BufferCapacity,
                config.Advanced);

            var serializers = CreateSerializers(config.Prefix, globalTags);
            var metricsSender = new MetricsSender(
                statsBufferize,
                new RandomGenerator(),
                new StopWatchFactory(),
                serializers,
                telemetry,
                config.StatsdTruncateIfTooLong);
            return new StatsdData(metricsSender, statsBufferize, transport, telemetry);
        }

        private static Serializers CreateSerializers(string prefix, string[] constantTags)
        {
            var serializerHelper = new SerializerHelper(constantTags);

            return new Serializers
            {
                MetricSerializer = new MetricSerializer(serializerHelper, prefix),
                ServiceCheckSerializer = new ServiceCheckSerializer(serializerHelper),
                EventSerializer = new EventSerializer(serializerHelper),
            };
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
            ITransport transport)
        {
            var telemetryFlush = config.Advanced.TelemetryFlushInterval;

            if (telemetryFlush.HasValue)
            {
                var assembly = typeof(StatsdBuilder).GetTypeInfo().Assembly;
                var version = assembly.GetName().Version.ToString();

                return _factory.CreateTelemetry(version, telemetryFlush.Value, transport, globalTags);
            }

            // Telemetry is not enabled
            return new Telemetry();
        }

        private TransportData CreateTransport(StatsdConfig config, string statsdServerName)
        {
            var transportData = new TransportData();

            if (statsdServerName.StartsWith(UnixDomainSocketPrefix))
            {
                statsdServerName = statsdServerName.Substring(UnixDomainSocketPrefix.Length);
                var endPoint = new UnixEndPoint(statsdServerName);
                transportData.Sender = _factory.CreateUnixDomainSocketTransport(
                    endPoint,
                    config.Advanced.UDSBufferFullBlockDuration);
                transportData.BufferCapacity = config.StatsdMaxUnixDomainSocketPacketSize;
            }
            else
            {
                transportData.Sender = CreateUDPTransport(config, statsdServerName);
                transportData.BufferCapacity = config.StatsdMaxUDPPacketSize;
            }

            return transportData;
        }

        private StatsBufferize CreateStatsBufferize(
            Telemetry telemetry,
            ITransport transport,
            int bufferCapacity,
            AdvancedStatsConfig config)
        {
            var bufferHandler = new BufferBuilderHandler(telemetry, transport);
            var bufferBuilder = new BufferBuilder(bufferHandler, bufferCapacity, "\n");

            var statsBufferize = _factory.CreateStatsBufferize(
                telemetry,
                bufferBuilder,
                config.MaxMetricsInAsyncQueue,
                config.MaxBlockDuration,
                config.DurationBeforeSendingNotFullBuffer);

            return statsBufferize;
        }

        private ITransport CreateUDPTransport(StatsdConfig config, string statsdServerName)
        {
            var address = StatsdUDP.GetIpv4Address(statsdServerName);
            var port = GetPort(config);

            var endPoint = new IPEndPoint(address, port);
            return _factory.CreateUDPTransport(endPoint);
        }

        private class TransportData
        {
            public ITransport Sender { get; set; }

            public int BufferCapacity { get; set; }
        }
    }
}
