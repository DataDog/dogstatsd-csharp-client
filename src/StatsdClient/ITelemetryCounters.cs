namespace StatsdClient
{
    /// <summary>
    /// ITelemetryCounters contains the telemetry counters.
    /// </summary>
    public interface ITelemetryCounters
    {
        int MetricsSent { get; }
        int EventsSent { get; }
        int ServiceChecksSent { get; }
        int BytesSent { get; }
        int BytesDropped { get; }
        int PacketsSent { get; }
        int PacketsDropped { get; }
        int PacketsDroppedQueue { get; }
    }
}