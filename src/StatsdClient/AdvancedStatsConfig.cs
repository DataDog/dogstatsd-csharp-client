using System;

namespace StatsdClient
{
    public class AdvancedStatsConfig
    {
        /// <summary>
        /// Metrics are sent asynchronously using a queue. 
        /// This value is the maximum number of metrics in the queue.
        /// A small value reduces memory usage whereas an higher value reduces 
        /// latency (When `MaxBlockDuration` is null) or the number of messages 
        /// dropped (When `MaxBlockDuration` is not null).
        /// </summary>
        public int MaxMetricsInAsyncQueue { get; set; } = 100 * 1000;

        /// <summary>
        /// If there are more metrics than `MaxMetricsInAsyncQueue` waiting to be sent:
        ///     - if `MaxBlockDuration` is null, the metric send by a call to a 
        ///       `DogStatsd` or `DogStatsdService` method will be dropped.
        ///     - If `MaxBlockDuration` is not null, the metric send by a call to a
        ///       `DogStatsd` or `DogStatsdService` method will block for at most
        ///       `MaxBlockDuration` duration.
        /// </summary>
        public TimeSpan? MaxBlockDuration { get; set; } = null;

        /// <summary>
        /// Metrics are buffered before sent. This value defined how long
        /// DogStatsD waits before sending a not full buffer.
        /// </summary>
        public TimeSpan DurationBeforeSendingNotFullBuffer { get; set; }

        /// <summary>
        /// This value defines how long to wait when the UDS buffer is full
        /// (SocketError.NoBufferSpaceAvailable). A null value results in
        /// dropping the metric.
        /// </summary>
        public TimeSpan? UDSBufferFullBlockDuration { get; set; } = null;

        /// <summary>
        /// This value defines the duration between two telemetry flushes.
        /// When this value is set at null, telemetry is disabled.
        /// </summary>
        public TimeSpan? TelemetryFlushInterval { get; set; } = TimeSpan.FromSeconds(10);

        public AdvancedStatsConfig()
        {
            DurationBeforeSendingNotFullBuffer = TimeSpan.FromMilliseconds(100);
        }
    }
}