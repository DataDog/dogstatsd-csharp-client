using NUnit.Framework;

namespace StatsdClient.Tests
{
    [TestFixture]
    public class MetricSerializerTests
    {
        // =-=-=-=- COUNTER -=-=-=-=

        [Test]
        public void SendIncreaseCounterByX()
        {
            AssertSerialize("counter:5|c", MetricType.Counting, "counter", 5);
        }

        [Test]
        public void SendDecreaseCounterByX()
        {
            AssertSerialize("counter:-5|c", MetricType.Counting, "counter", -5);
        }

        [Test]
        public void SendIncreaseCounterByXAndTags()
        {
            AssertSerialize(
                "counter:5|c|#tag1:true,tag2",
                MetricType.Counting,
                "counter",
                5,
                tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void SendIncreaseCounterByXAndSampleRate()
        {
            AssertSerialize("counter:5|c|@0.1", MetricType.Counting, "counter", 5, sampleRate: 0.1);
        }

        [Test]
        public void SendIncreaseCounterByXAndSampleRateAndTags()
        {
            AssertSerialize(
                "counter:5|c|@0.1|#tag1:true,tag2",
                MetricType.Counting,
                "counter",
                5,
                sampleRate: 0.1,
                tags: new[] { "tag1:true", "tag2" });
        }

        // =-=-=-=- TIMER -=-=-=-=

        [Test]
        public void SendTimer()
        {
            AssertSerialize("timer:5|ms", MetricType.Timing, "timer", 5);
        }

        [Test]
        public void SendTimerDouble()
        {
            AssertSerialize("timer:5.5|ms", MetricType.Timing, "timer", 5.5);
        }

        [Test]
        public void SendTimerWithTags()
        {
            AssertSerialize("timer:5|ms|#tag1:true", MetricType.Timing, "timer", 5, tags: new[] { "tag1:true" });
        }

        [Test]
        public void SendTimerWithSampleRate()
        {
            AssertSerialize("timer:5|ms|@0.5", MetricType.Timing, "timer", 5, sampleRate: 0.5);
        }

        [Test]
        public void SendTimerWithSampleRateAndTags()
        {
            AssertSerialize(
                "timer:5|ms|@0.5|#tag1:true,tag2",
                MetricType.Timing,
                "timer",
                5,
                sampleRate: 0.5,
                tags: new[] { "tag1:true", "tag2" });
        }

        // =-=-=-=- GAUGE -=-=-=-=

        [Test]
        public void SendGauge()
        {
            AssertSerialize("gauge:5|g", MetricType.Gauge, "gauge", 5);
        }

        [Test]
        public void SendGaugeWithDouble()
        {
            AssertSerialize("gauge:4.2|g", MetricType.Gauge, "gauge", 4.2);
        }

        [Test]
        public void SendGaugeWithTags()
        {
            AssertSerialize(
                "gauge:5|g|#tag1:true,tag2",
                MetricType.Gauge,
                "gauge",
                5,
                tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void SendGaugeWithSampleRate()
        {
            AssertSerialize("gauge:5|g|@0.5", MetricType.Gauge, "gauge", 5, sampleRate: 0.5);
        }

        [Test]
        public void SendGaugeWithSampleRateAndTags()
        {
            AssertSerialize(
                "gauge:5|g|@0.5|#tag1:true,tag2",
                MetricType.Gauge,
                "gauge",
                5,
                sampleRate: 0.5,
                tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void SendGaugeWithSampleRateAndTagsDouble()
        {
            AssertSerialize(
                "gauge:5.4|g|@0.5|#tag1:true,tag2",
                MetricType.Gauge,
                "gauge",
                5.4,
                sampleRate: 0.5,
                tags: new[] { "tag1:true", "tag2" });
        }

        // =-=-=-=- PREFIX -=-=-=-=

        [Test]
        public void SetPrefixOnStatsNameWhenCallingSend()
        {
            AssertSerialize("a.prefix.counter:5|c", MetricType.Counting, "counter", 5, prefix: "a.prefix");
        }

        // DOGSTATSD-SPECIFIC

        // =-=-=-=- HISTOGRAM -=-=-=-=
        [Test]
        public void SendHistogram()
        {
            AssertSerialize("histogram:5|h", MetricType.Histogram, "histogram", 5);
        }

        [Test]
        public void SendHistogramDouble()
        {
            AssertSerialize("histogram:5.3|h", MetricType.Histogram, "histogram", 5.3);
        }

        [Test]
        public void SendHistogramWithTags()
        {
            AssertSerialize(
                "histogram:5|h|#tag1:true,tag2",
                MetricType.Histogram,
                "histogram",
                5,
                tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void SendHistogramWithSampleRate()
        {
            AssertSerialize("histogram:5|h|@0.5", MetricType.Histogram, "histogram", 5, sampleRate: 0.5);
        }

        [Test]
        public void SendHistogramWithSampleRateAndTags()
        {
            AssertSerialize(
                "histogram:5|h|@0.5|#tag1:true,tag2",
                MetricType.Histogram,
                "histogram",
                5,
                sampleRate: 0.5,
                tags: new[] { "tag1:true", "tag2" });
        }

        // =-=-=-=- DISTRIBUTION -=-=-=-=
        [Test]
        public void SendDistribution()
        {
            AssertSerialize("distribution:5|d", MetricType.Distribution, "distribution", 5);
        }

        [Test]
        public void SendDistributionDouble()
        {
            AssertSerialize("distribution:5.3|d", MetricType.Distribution, "distribution", 5.3);
        }

        [Test]
        public void SendDistributionWithTags()
        {
            AssertSerialize(
                "distribution:5|d|#tag1:true,tag2",
                MetricType.Distribution,
                "distribution",
                5,
                tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void SendDistributionWithSampleRate()
        {
            AssertSerialize(
                "distribution:5|d|@0.5",
                MetricType.Distribution,
                "distribution",
                5,
                sampleRate: 0.5);
        }

        [Test]
        public void SendDistributionWithSampleRateAndTags()
        {
            AssertSerialize(
                "distribution:5|d|@0.5|#tag1:true,tag2",
                MetricType.Distribution,
                "distribution",
                5,
                sampleRate: 0.5,
                tags: new[] { "tag1:true", "tag2" });
        }

        // =-=-=-=- SET -=-=-=-=
        [Test]
        public void SendSet()
        {
            AssertSerialize("set:5|s", MetricType.Set, "set", 5);
        }

        [Test]
        public void SendSetString()
        {
            AssertSerialize("set:objectname|s", MetricType.Set, "set", "objectname");
        }

        [Test]
        public void SendSetWithTags()
        {
            AssertSerialize("set:5|s|#tag1:true,tag2", MetricType.Set, "set", 5, tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void SendSetWithSampleRate()
        {
            AssertSerialize("set:5|s|@0.1", MetricType.Set, "set", 5, sampleRate: 0.1);
        }

        [Test]
        public void SendSetWithSampleRateAndTags()
        {
            AssertSerialize(
                "set:5|s|@0.1|#tag1:true,tag2",
                MetricType.Set,
                "set",
                5,
                sampleRate: 0.1,
                tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void SendSetStringWithSampleRateAndTags()
        {
            AssertSerialize(
                "set:objectname|s|@0.1|#tag1:true,tag2",
                MetricType.Set,
                "set",
                "objectname",
                sampleRate: 0.1,
                tags: new[] { "tag1:true", "tag2" });
        }

        private static void AssertSerialize<T>(
            string expectValue,
            MetricType metricType,
            string name,
            T value,
            double sampleRate = 1.0,
            string[] tags = null,
            string prefix = null)
        {
            var serializerHelper = new SerializerHelper(null, 10);
            var serializer = new MetricSerializer(serializerHelper, prefix);
            var serializedMetric = serializer.Serialize(
                metricType,
                name,
                value,
                sampleRate,
                tags);
            Assert.AreEqual(expectValue, serializedMetric.ToString());
        }
    }
}