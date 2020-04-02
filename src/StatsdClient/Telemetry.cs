using System;
using System.Threading;
using StatsdClient.Bufferize;

namespace StatsdClient
{
    /// <summary>
    /// Telemetry sends telemetry metrics.
    /// </summary>
    internal class Telemetry : IDisposable
    {
        private int _metricsSent;
        private int _eventsSent;
        private int _serviceChecksSent;
        private int _bytesSent;
        private int _bytesDropped;
        private int _packetsSent;
        private int _packetsDropped;
        private int _packetsDroppedQueue;

        private readonly Timer _timer;
        private readonly string[] _tags;
        private readonly IStatsSender _statsSender;

        private static string _telemetryPrefix = "datadog.dogstatsd.client.";

        public static string MetricsMetricName = _telemetryPrefix + "metrics";
        public static string EventsMetricName = _telemetryPrefix + "events";
        public static string ServiceCheckMetricName = _telemetryPrefix + "service_checks";
        public static string BytesSentMetricName = _telemetryPrefix + "bytes_sent";
        public static string BytesDroppedMetricName = _telemetryPrefix + "bytes_dropped";
        public static string PacketsSentMetricName = _telemetryPrefix + "packets_sent";
        public static string PacketsDroppedMetricName = _telemetryPrefix + "packets_dropped";
        public static string PacketsDroppedQueueMetricName = _telemetryPrefix + "packets_dropped_queue";

        /// <summary>
        /// This constructor does not send telemetry.
        /// </summary>
        public Telemetry() { }

        public Telemetry(string assemblyVersion, TimeSpan flushInterval, IStatsSender statsSender)
        {
            _statsSender = statsSender;

            string transport;
            switch (statsSender.TransportType)
            {
                case StatsSenderTransportType.UDP: transport = "udp"; break;
                case StatsSenderTransportType.UDS: transport = "uds"; break;
                default: transport = statsSender.TransportType.ToString(); break;
            };

            _tags = new[] { "client:csharp", $"client_version:{assemblyVersion}", $"client_transport:{transport}" };

            _timer = new Timer(_ => Flush(),
                               null,
                               flushInterval,
                               flushInterval);
        }

        public void Flush()
        {
            SendMetric(MetricsMetricName, Interlocked.Exchange(ref _metricsSent, 0));
            SendMetric(EventsMetricName, Interlocked.Exchange(ref _eventsSent, 0));
            SendMetric(ServiceCheckMetricName, Interlocked.Exchange(ref _serviceChecksSent, 0));
            SendMetric(BytesSentMetricName, Interlocked.Exchange(ref _bytesSent, 0));
            SendMetric(BytesDroppedMetricName, Interlocked.Exchange(ref _bytesDropped, 0));
            SendMetric(PacketsSentMetricName, Interlocked.Exchange(ref _packetsSent, 0));
            SendMetric(PacketsDroppedMetricName, Interlocked.Exchange(ref _packetsDropped, 0));
            SendMetric(PacketsDroppedQueueMetricName, Interlocked.Exchange(ref _packetsDroppedQueue, 0));
        }

        void SendMetric(string metricName, int value)
        {
            var message = Statsd.Metric.GetCommand<Statsd.Counting, int>(string.Empty,
                                                                         metricName,
                                                                         value,
                                                                         1.0,
                                                                         _tags);
            var bytes = BufferBuilder.GetBytes(message);
            _statsSender.Send(bytes, bytes.Length);
        }

        public void OnMetricSent() { Interlocked.Increment(ref _metricsSent); }
        public void OnEventSent() { Interlocked.Increment(ref _eventsSent); }
        public void OnServiceCheckSent() { Interlocked.Increment(ref _serviceChecksSent); }

        public void OnPacketSent(int packetSize)
        {
            Interlocked.Increment(ref _packetsSent);
            Interlocked.Add(ref _bytesSent, packetSize);
        }

        public void OnPacketDropped(int packetSize)
        {
            Interlocked.Increment(ref _packetsDropped);
            Interlocked.Add(ref _bytesDropped, packetSize);
        }

        public void OnPacketsDroppedQueue() { Interlocked.Increment(ref _packetsDroppedQueue); }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}