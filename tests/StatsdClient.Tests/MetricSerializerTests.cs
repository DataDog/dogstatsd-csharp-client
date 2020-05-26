using NUnit.Framework;

namespace StatsdClient.Tests
{
    [TestFixture]
    public class MetricSerializerTests
    {
        // =-=-=-=- COUNTER -=-=-=-=

        [Test]
        public void Send_increase_counter_by_x()
        {
            AssertSerialize("counter:5|c", MetricType.Counting, "counter", 5);
        }

        [Test]
        public void Send_decrease_counter_by_x()
        {
            AssertSerialize("counter:-5|c", MetricType.Counting, "counter", -5);
        }

        [Test]
        public void Send_increase_counter_by_x_and_tags()
        {
            AssertSerialize(
                "counter:5|c|#tag1:true,tag2",
                MetricType.Counting,
                "counter",
                5,
                tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void Send_increase_counter_by_x_and_sample_rate()
        {
            AssertSerialize("counter:5|c|@0.1", MetricType.Counting, "counter", 5, sampleRate: 0.1);
        }

        [Test]
        public void Send_increase_counter_by_x_and_sample_rate_and_tags()
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
        public void Send_timer()
        {
            AssertSerialize("timer:5|ms", MetricType.Timing, "timer", 5);
        }

        [Test]
        public void Send_timer_double()
        {
            AssertSerialize("timer:5.5|ms", MetricType.Timing, "timer", 5.5);
        }

        [Test]
        public void Send_timer_with_tags()
        {
            AssertSerialize("timer:5|ms|#tag1:true", MetricType.Timing, "timer", 5, tags: new[] { "tag1:true" });
        }

        [Test]
        public void Send_timer_with_sample_rate()
        {
            AssertSerialize("timer:5|ms|@0.5", MetricType.Timing, "timer", 5, sampleRate: 0.5);
        }

        [Test]
        public void Send_timer_with_sample_rate_and_tags()
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
        public void Send_gauge()
        {
            AssertSerialize("gauge:5|g", MetricType.Gauge, "gauge", 5);
        }

        [Test]
        public void Send_gauge_with_double()
        {
            AssertSerialize("gauge:4.2|g", MetricType.Gauge, "gauge", 4.2);
        }

        [Test]
        public void Send_gauge_with_tags()
        {
            AssertSerialize(
                "gauge:5|g|#tag1:true,tag2",
                MetricType.Gauge,
                "gauge",
                5,
                tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void Send_gauge_with_sample_rate()
        {
            AssertSerialize("gauge:5|g|@0.5", MetricType.Gauge, "gauge", 5, sampleRate: 0.5);
        }

        [Test]
        public void Send_gauge_with_sample_rate_and_tags()
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
        public void Send_gauge_with_sample_rate_and_tags_double()
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
        public void Set_prefix_on_stats_name_when_calling_send()
        {
            AssertSerialize("a.prefix.counter:5|c", MetricType.Counting, "counter", 5, prefix: "a.prefix");
        }

        // DOGSTATSD-SPECIFIC

        // =-=-=-=- HISTOGRAM -=-=-=-=
        [Test]
        public void Send_histogram()
        {
            AssertSerialize("histogram:5|h", MetricType.Histogram, "histogram", 5);
        }

        [Test]
        public void Send_histogram_double()
        {
            AssertSerialize("histogram:5.3|h", MetricType.Histogram, "histogram", 5.3);
        }

        [Test]
        public void Send_histogram_with_tags()
        {
            AssertSerialize(
                "histogram:5|h|#tag1:true,tag2",
                MetricType.Histogram,
                "histogram",
                5,
                tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void Send_histogram_with_sample_rate()
        {
            AssertSerialize("histogram:5|h|@0.5", MetricType.Histogram, "histogram", 5, sampleRate: 0.5);
        }

        [Test]
        public void Send_histogram_with_sample_rate_and_tags()
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
        public void Send_distribution()
        {
            AssertSerialize("distribution:5|d", MetricType.Distribution, "distribution", 5);
        }

        [Test]
        public void Send_distribution_double()
        {
            AssertSerialize("distribution:5.3|d", MetricType.Distribution, "distribution", 5.3);
        }

        [Test]
        public void Send_distribution_with_tags()
        {
            AssertSerialize(
                "distribution:5|d|#tag1:true,tag2",
                MetricType.Distribution,
                "distribution",
                5,
                tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void Send_distribution_with_sample_rate()
        {
            AssertSerialize(
                "distribution:5|d|@0.5",
                MetricType.Distribution,
                "distribution",
                5,
                sampleRate: 0.5);
        }

        [Test]
        public void Send_distribution_with_sample_rate_and_tags()
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
        public void Send_set()
        {
            AssertSerialize("set:5|s", MetricType.Set, "set", 5);
        }

        [Test]
        public void Send_set_string()
        {
            AssertSerialize("set:objectname|s", MetricType.Set, "set", "objectname");
        }

        [Test]
        public void Send_set_with_tags()
        {
            AssertSerialize("set:5|s|#tag1:true,tag2", MetricType.Set, "set", 5, tags: new[] { "tag1:true", "tag2" });
        }

        [Test]
        public void Send_set_with_sample_rate()
        {
            AssertSerialize("set:5|s|@0.1", MetricType.Set, "set", 5, sampleRate: 0.1);
        }

        [Test]
        public void Send_set_with_sample_rate_and_tags()
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
        public void Send_set_string_with_sample_rate_and_tags()
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
            var serializer = CreateSerializer(prefix);
            var rawMetric = serializer.Serialize(
                metricType,
                name,
                value,
                sampleRate,
                tags);
            Assert.AreEqual(expectValue, rawMetric);
        }

        private static MetricSerializer CreateSerializer(string prefix = null)
        {
            return new MetricSerializer(prefix, null);
        }
    }
}