using BenchmarkDotNet.Attributes;
using StatsdClient.Statistic;

namespace StatsdClient.Benchmarks
{
    [MemoryDiagnoser]
    public class MetricSerializerBenchmark
    {
        private MetricSerializer _metricSerializer;
        private StatsMetric _metricStats;
        private SerializedMetric _serializedMetric;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var serializerHelper = new SerializerHelper(new[] { "constant_tags" });
            _metricSerializer = new MetricSerializer(serializerHelper, "prefix");
            _serializedMetric = new SerializedMetric();
            _metricStats = new StatsMetric
            {
                MetricType = MetricType.Count,
                StatName = "StatsdClient.Benchmarks.SerializeTo",
                NumericValue = 42,
                SampleRate = 1,
                Tags = new[] { "TAG1", "TAG2", "TAG3" },
            };
        }

        [Benchmark]
        public void SerializeTo()
        {
            _metricSerializer.SerializeTo(ref _metricStats, _serializedMetric);
        }
    }
}