using System;
using StatsdClient.Bufferize;

namespace StatsdClient
{
    internal class MetricsSender
    {
        private readonly Telemetry _optionalTelemetry;
        private readonly StatsBufferize _statsBufferize;
        private readonly MetricSerializer _metricSerializer;

        internal MetricsSender(
                    StatsBufferize statsBufferize,
                    IRandomGenerator randomGenerator,
                    IStopWatchFactory stopwatchFactory,
                    MetricSerializer metricSerializer,
                    Telemetry optionalTelemetry)
        {
            StopwatchFactory = stopwatchFactory;
            _statsBufferize = statsBufferize;
            RandomGenerator = randomGenerator;
            _optionalTelemetry = optionalTelemetry;
            _metricSerializer = metricSerializer;
        }

        public bool TruncateIfTooLong { get; set; }

        private IStopWatchFactory StopwatchFactory { get; set; }

        private IRandomGenerator RandomGenerator { get; set; }

        public void SendEvent(string title, string text, string alertType = null, string aggregationKey = null, string sourceType = null, int? dateHappened = null, string priority = null, string hostname = null, string[] tags = null, bool truncateIfTooLong = false)
        {
            truncateIfTooLong = truncateIfTooLong || TruncateIfTooLong;
            _statsBufferize.Send(_metricSerializer.SerializeEvent(title, text, alertType, aggregationKey, sourceType, dateHappened, priority, hostname, tags, truncateIfTooLong));
            _optionalTelemetry?.OnEventSent();
        }

        public void SendServiceCheck(string name, int status, int? timestamp = null, string hostname = null, string[] tags = null, string serviceCheckMessage = null, bool truncateIfTooLong = false)
        {
            truncateIfTooLong = truncateIfTooLong || TruncateIfTooLong;
            _statsBufferize.Send(_metricSerializer.SerializeServiceCheck(name, status, timestamp, hostname, tags, serviceCheckMessage, truncateIfTooLong));
            _optionalTelemetry?.OnServiceCheckSent();
        }

        public void SendMetric<T>(MetricType metricType, string name, T value, double sampleRate = 1.0, string[] tags = null)
        {
            if (RandomGenerator.ShouldSend(sampleRate))
            {
                _statsBufferize.Send(_metricSerializer.SerializeMetric(metricType, name, value, sampleRate, tags));
                _optionalTelemetry?.OnMetricSent();
            }
        }

        public void Send(Action actionToTime, string statName, double sampleRate = 1.0, string[] tags = null)
        {
            var stopwatch = StopwatchFactory.Get();

            try
            {
                stopwatch.Start();
                actionToTime();
            }
            finally
            {
                stopwatch.Stop();
                SendMetric(MetricType.Timing, statName, stopwatch.ElapsedMilliseconds(), sampleRate, tags);
            }
        }
    }
}
