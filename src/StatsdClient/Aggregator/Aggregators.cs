namespace StatsdClient.Aggregator
{
    /// <summary>
    /// Store all aggregators
    /// </summary>
    internal class Aggregators
    {
        public CountingAggregator OptionalCounting { get; set; }

        public GaugeAggregator OptionalGauge { get; set; }

        public SetAggregator OptionalSet { get; set; }
    }
}