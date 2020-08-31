namespace StatsdClient.Aggregator
{
    /// <summary>
    /// Store all aggregators
    /// </summary>
    internal class Aggregators
    {
        public CountAggregator OptionalCount { get; set; }

        public GaugeAggregator OptionalGauge { get; set; }

        public SetAggregator OptionalSet { get; set; }
    }
}