using System;
using NUnit.Framework;
using StatsdClient.Statistic;

namespace StatsdClient.Tests
{
    [TestFixture]
    public class MetricSerializerTests
    {
        // =-=-=-=- COUNTER -=-=-=-=

        [Test]
        public void SendIncreaseCounterByX()
        {
            AssertSerialize("counter:5|c", MetricType.Count, "counter", 5);
        }

        [Test]
        public void SendDecreaseCounterByX()
        {
            AssertSerialize("counter:-5|c", MetricType.Count, "counter", -5);
        }

        [Test]
        public void SendIncreaseCounterByXAndTags()
        {
            AssertSerialize(
                "counter:5|c|#tag1:true,tag2",
                MetricType.Count,
                "counter",
                5,
                tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void SendIncreaseCounterByXAndSampleRate()
        {
            AssertSerialize("counter:5|c|@0.1", MetricType.Count, "counter", 5, sampleRate: 0.1);
        }

        [Test]
        public void SendIncreaseCounterByXAndSampleRateAndTags()
        {
            AssertSerialize(
                "counter:5|c|@0.1|#tag1:true,tag2",
                MetricType.Count,
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
            AssertSerialize("a.prefix.counter:5|c", MetricType.Count, "counter", 5, prefix: "a.prefix");
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
            AssertSetSerialize("set:5|s", "set", 5);
        }

        [Test]
        public void SendSetString()
        {
            AssertSetSerialize("set:objectname|s", "set", "objectname");
        }

        [Test]
        public void SendSetWithTags()
        {
            AssertSetSerialize("set:5|s|#tag1:true,tag2", "set", 5, tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void SendSetWithSampleRate()
        {
            AssertSetSerialize("set:5|s|@0.1", "set", 5, sampleRate: 0.1);
        }

        [Test]
        public void SendSetWithSampleRateAndTags()
        {
            AssertSetSerialize(
                "set:5|s|@0.1|#tag1:true,tag2",
                "set",
                5,
                sampleRate: 0.1,
                tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void SendSetStringWithSampleRateAndTags()
        {
            AssertSetSerialize(
                "set:objectname|s|@0.1|#tag1:true,tag2",
                "set",
                "objectname",
                sampleRate: 0.1,
                tags: new[] { "tag1:true", "tag2" });
        }

        private static void AssertSerialize(
            string expectValue,
            MetricType metricType,
            string name,
            double value,
            double sampleRate = 1.0,
            string[] tags = null,
            string prefix = null)
        {
            var statsMetric = new StatsMetric
            {
                MetricType = metricType,
                StatName = name,
                SampleRate = sampleRate,
                NumericValue = value,
                Tags = tags,
            };
            AssertSerialize(expectValue, ref statsMetric, prefix);
        }

        private static void AssertSetSerialize(
           string expectValue,
           string name,
           object value,
           double sampleRate = 1.0,
           string[] tags = null,
           string prefix = null)
        {
            var statsMetric = new StatsMetric
            {
                MetricType = MetricType.Set,
                StatName = name,
                SampleRate = sampleRate,
                StringValue = value.ToString(),
                Tags = tags,
            };
            AssertSerialize(expectValue, ref statsMetric, prefix);
        }

        private static void AssertSerialize(
           string expectValue,
           ref StatsMetric statsMetric,
           string prefix)
        {
            var serializerHelper = new SerializerHelper(null);
            var serializer = new MetricSerializer(serializerHelper, prefix);
            var serializedMetric = new SerializedMetric();
            serializer.SerializeTo(ref statsMetric, serializedMetric);
            Assert.AreEqual(expectValue, serializedMetric.ToString());
        }
    }
}