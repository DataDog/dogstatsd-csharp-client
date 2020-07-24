using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Unix;
using StatsdClient.Bufferize;
using StatsdClient.Transport;

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
            var endPoint = BuildEndPoint(config);
            var transportData = CreateTransportData(endPoint, config);
            var transport = transportData.Transport;
            var globalTags = GetGlobalTags(config);
            var telemetry = CreateTelemetry(config, globalTags, endPoint, transportData.Transport);
            var statsBufferize = CreateStatsBufferize(
                telemetry,
                transportData.Transport,
                transportData.BufferCapacity,
                config.Advanced);

            var serializers = CreateSerializers(config.Prefix, globalTags, config.Advanced.MaxMetricsInAsyncQueue);
            var metricsSender = new MetricsSender(
                statsBufferize,
                new RandomGenerator(),
                new StopWatchFactory(),
                serializers,
                telemetry,
                config.StatsdTruncateIfTooLong);
            return new StatsdData(metricsSender, statsBufferize, transport, telemetry);
        }

        private static DogStatsdEndPoint BuildEndPoint(StatsdConfig config)
        {
            var statsdServerName = !string.IsNullOrEmpty(config.StatsdServerName)
                            ? config.StatsdServerName
                            : Environment.GetEnvironmentVariable(StatsdConfig.DD_AGENT_HOST_ENV_VAR);

            var pipeName = config.PipeName;
            if (string.IsNullOrEmpty(statsdServerName) && string.IsNullOrEmpty(pipeName))
            {
                // Ignore pipe name in the error message as its usage is internal only.
                throw new ArgumentNullException(
                    $"{nameof(config)}.{nameof(config.StatsdServerName)} and"
                    + $" {StatsdConfig.DD_AGENT_HOST_ENV_VAR} environment variable not set");
            }

            return new DogStatsdEndPoint
            {
                ServerName = statsdServerName,
                PipeName = pipeName,
                Port = GetPort(config.StatsdPort),
            };
        }

        private static Serializers CreateSerializers(
            string prefix,
            string[] constantTags,
            int maxMetricsInAsyncQueue)
        {
            // 100 is an arbitrary value. poolMaxAllocation must be a little greater than maxMetricsInAsyncQueue.
            var poolMaxAllocation = maxMetricsInAsyncQueue + 1000;
            var serializerHelper = new SerializerHelper(constantTags, poolMaxAllocation);

            return new Serializers
            {
                MetricSerializer = new MetricSerializer(serializerHelper, prefix),
                ServiceCheckSerializer = new ServiceCheckSerializer(serializerHelper),
                EventSerializer = new EventSerializer(serializerHelper),
            };
        }

        private static int GetPort(int statsdPort)
        {
            if (statsdPort != 0)
            {
                return statsdPort;
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
            DogStatsdEndPoint dogStatsdEndPoint,
            ITransport transport)
        {
            var telemetryFlush = config.Advanced.TelemetryFlushInterval;

            if (telemetryFlush.HasValue)
            {
                var assembly = typeof(StatsdBuilder).GetTypeInfo().Assembly;
                var version = assembly.GetName().Version.ToString();
                var optionalTelemetryEndPoint = config.Advanced.OptionalTelemetryEndPoint;
                ITransport telemetryTransport = transport;
                if (optionalTelemetryEndPoint != null && !dogStatsdEndPoint.AreEquals(optionalTelemetryEndPoint))
                {
                    telemetryTransport = CreateTransport(optionalTelemetryEndPoint, config);
                }

                return _factory.CreateTelemetry(version, telemetryFlush.Value, telemetryTransport, globalTags);
            }

            // Telemetry is not enabled
            return new Telemetry();
        }

        private ITransport CreateTransport(DogStatsdEndPoint endPoint, StatsdConfig config)
        {
            var serverName = endPoint.ServerName;
            if (!string.IsNullOrEmpty(serverName))
            {
                if (serverName.StartsWith(UnixDomainSocketPrefix))
                {
                    serverName = serverName.Substring(UnixDomainSocketPrefix.Length);
                    var unixEndPoint = new UnixEndPoint(serverName);
                    return _factory.CreateUnixDomainSocketTransport(
                        unixEndPoint,
                        config.Advanced.UDSBufferFullBlockDuration);
                }

                return CreateUDPTransport(endPoint);
            }

            var pipeName = endPoint.PipeName;
            if (string.IsNullOrEmpty(pipeName))
            {
                throw new ArgumentException($"Error: empty {nameof(DogStatsdEndPoint)}");
            }

            return _factory.CreateNamedPipeTransport(pipeName);
        }

        private TransportData CreateTransportData(DogStatsdEndPoint endPoint, StatsdConfig config)
        {
            var transportData = new TransportData();

            transportData.Transport = CreateTransport(endPoint, config);
            switch (transportData.Transport.TransportType)
            {
                case TransportType.UDP: transportData.BufferCapacity = config.StatsdMaxUDPPacketSize; break;
                case TransportType.UDS: transportData.BufferCapacity = config.StatsdMaxUnixDomainSocketPacketSize; break;
                // use StatsdMaxUDPPacketSize for named pipe
                case TransportType.NamedPipe: transportData.BufferCapacity = config.StatsdMaxUDPPacketSize; break;
                default: throw new NotSupportedException();
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
                bufferBuilder,
                config.MaxMetricsInAsyncQueue,
                config.MaxBlockDuration,
                config.DurationBeforeSendingNotFullBuffer);

            return statsBufferize;
        }

        private ITransport CreateUDPTransport(DogStatsdEndPoint endPoint)
        {
            var address = StatsdUDP.GetIpv4Address(endPoint.ServerName);
            var port = endPoint.Port;

            var ipEndPoint = new System.Net.IPEndPoint(address, port);
            return _factory.CreateUDPTransport(ipEndPoint);
        }

        private class TransportData
        {
            public ITransport Transport { get; set; }

            public int BufferCapacity { get; set; }
        }
    }
}
