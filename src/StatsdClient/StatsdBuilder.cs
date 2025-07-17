using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Unix;
using StatsdClient.Aggregator;
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

        public StatsdData BuildStatsData(StatsdConfig config, Action<Exception> optionalExceptionHandler)
        {
            var endPoint = BuildEndPoint(config);
            var transportData = CreateTransportData(endPoint, config);
            var transport = transportData.Transport;
            var globalTags = GetGlobalTags(config);
            var originDetectionEnabled = IsOriginDetectionEnabled(config);
            var serializers = CreateSerializers(config.Prefix, globalTags, config.Advanced.MaxMetricsInAsyncQueue, originDetectionEnabled, config.ContainerID);
            var telemetry = CreateTelemetry(serializers.MetricSerializer, config, globalTags, endPoint, transportData.Transport, optionalExceptionHandler);
            var statsBufferize = CreateStatsBufferize(
                telemetry,
                transportData.Transport,
                transportData.BufferCapacity,
                config.Advanced,
                serializers,
                config.ClientSideAggregation,
                optionalExceptionHandler);

            var metricsSender = new MetricsSender(
                 statsBufferize,
                 new RandomGenerator(),
                 new StopWatchFactory(),
                 telemetry,
                 config.StatsdTruncateIfTooLong);
            return new StatsdData(metricsSender, statsBufferize, transport, telemetry);
        }

        private static bool IsOriginDetectionEnabled(StatsdConfig config)
        {
            if (config.OriginDetection.HasValue && !config.OriginDetection.Value)
            {
                return false;
            }

            var value = Environment.GetEnvironmentVariable(StatsdConfig.OriginDetectionEnabledEnvVar);
            if (!string.IsNullOrEmpty(value))
            {
                return IsTrue(value);
            }

            // Defaults to enabled.
            return true;
        }


        private static bool IsTrue(string value)
        {
            return !(value.ToLower() == "0" ||
                 value.ToLower() == "f" ||
                 value.ToLower() == "false");
        }

        private static void AddTag(List<string> tags, string tagKey, string environmentVariableName, string originalValue = null)
        {
            var value = originalValue;

            if (string.IsNullOrEmpty(value))
            {
                value = Environment.GetEnvironmentVariable(environmentVariableName);
            }

            if (!string.IsNullOrEmpty(value))
            {
                tags.Add($"{tagKey}:{value}");
            }
        }

        private static DogStatsdEndPoint BuildEndPoint(StatsdConfig config)
        {
            var statsdServerName = !string.IsNullOrEmpty(config.StatsdServerName)
                            ? config.StatsdServerName
                            : Environment.GetEnvironmentVariable(StatsdConfig.AgentHostEnvVar);

            var pipeName = !string.IsNullOrEmpty(config.PipeName)
                            ? config.PipeName
                            : Environment.GetEnvironmentVariable(StatsdConfig.AgentPipeNameEnvVar);

            if (string.IsNullOrEmpty(statsdServerName) && string.IsNullOrEmpty(pipeName))
            {
                // Ignore pipe name in the error message as its usage is internal only.
                throw new ArgumentNullException(
                    $"{nameof(config)}.{nameof(config.StatsdServerName)} and"
                    + $" {StatsdConfig.AgentHostEnvVar} environment variable not set");
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
            int maxMetricsInAsyncQueue,
            bool originDetectionEnabled,
            string containerID)
        {
            //var containerId = originDetection.GetContainerID(containerID, originDetectionEnabled);
            var originDetection = new OriginDetection(new FileSystem(), containerID, originDetectionEnabled);
            var serializerHelper = new SerializerHelper(constantTags, originDetection);

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

            var portString = Environment.GetEnvironmentVariable(StatsdConfig.DogStatsdPortEnvVar);
            if (!string.IsNullOrEmpty(portString))
            {
                if (int.TryParse(portString, out var port))
                {
                    return port;
                }

                throw new ArgumentException($"Environment Variable '{StatsdConfig.DogStatsdPortEnvVar}' bad format: {portString}");
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

            AddTag(globalTags, _entityIdInternalTagKey, StatsdConfig.EntityIdEnvVar);
            AddTag(globalTags, StatsdConfig.ServiceTagKey, StatsdConfig.ServiceEnvVar, config.ServiceName);
            AddTag(globalTags, StatsdConfig.EnvironmentTagKey, StatsdConfig.EnvironmentEnvVar, config.Environment);
            AddTag(globalTags, StatsdConfig.VersionTagKey, StatsdConfig.VersionEnvVar, config.ServiceVersion);

            return globalTags.ToArray();
        }

        private Telemetry CreateTelemetry(
            MetricSerializer metricSerializer,
            StatsdConfig config,
            string[] globalTags,
            DogStatsdEndPoint dogStatsdEndPoint,
            ITransport transport,
            Action<Exception> optionalExceptionHandler)
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

                return _factory.CreateTelemetry(metricSerializer, version, telemetryFlush.Value, telemetryTransport, globalTags, optionalExceptionHandler);
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
            AdvancedStatsConfig config,
            Serializers serializers,
            ClientSideAggregationConfig optionalClientSideAggregationConfig,
            Action<Exception> optionalExceptionHandler)
        {
            var bufferHandler = new BufferBuilderHandler(telemetry, transport);
            var bufferBuilder = new BufferBuilder(bufferHandler, bufferCapacity, "\n", optionalExceptionHandler);

            Aggregators optionalAggregators = null;
            if (optionalClientSideAggregationConfig != null)
            {
                var parameters = new MetricAggregatorParameters(
                    serializers.MetricSerializer,
                    bufferBuilder,
                    optionalClientSideAggregationConfig.MaxUniqueStatsBeforeFlush,
                    optionalClientSideAggregationConfig.FlushInterval,
                    telemetry);

                optionalAggregators = new Aggregators
                {
                    OptionalCount = new CountAggregator(parameters),
                    OptionalGauge = new GaugeAggregator(parameters),
                    OptionalSet = new SetAggregator(parameters, telemetry, optionalExceptionHandler),
                };
            }

            var statsRouter = _factory.CreateStatsRouter(serializers, bufferBuilder, optionalAggregators);

            var statsBufferize = _factory.CreateStatsBufferize(
                statsRouter,
                config.MaxMetricsInAsyncQueue,
                config.MaxBlockDuration,
                config.DurationBeforeSendingNotFullBuffer,
                optionalExceptionHandler);

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
