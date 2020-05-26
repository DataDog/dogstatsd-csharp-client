namespace StatsdClient
{
    internal enum MetricType
    {
        Counting,
        Timing,
        Gauge,
        Histogram,
        Distribution,
        Meter,
        Set,
    }
}