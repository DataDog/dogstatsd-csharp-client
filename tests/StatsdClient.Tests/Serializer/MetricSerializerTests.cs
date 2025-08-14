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

        [Test]
        public void SendCounterWithTimestamp()
        {
            var dto = new DateTimeOffset(2013, 05, 01, 18, 30, 00, new TimeSpan(0, 0, 0));
            AssertSerialize(
                "the.counter:5|c|T1367433000",
                MetricType.Count,
                "the.counter",
                5,
                timestamp: dto);
        }

        [Test]
        public void SendCounterWithTagsAndTimestamp()
        {
            var dto = new DateTimeOffset(2013, 05, 01, 18, 30, 00, new TimeSpan(0, 0, 0));
            AssertSerialize(
                "the.counter:5|c|#tag1:true,tag2|T1367433000",
                MetricType.Count,
                "the.counter",
                5,
                tags: new[] { "tag1:true", "tag2" },
                timestamp: dto);
        }

        [Test]
        public void SendCounterWithTagsAndTimestampAndSampleRate()
        {
            var dto = new DateTimeOffset(2013, 05, 01, 18, 30, 00, new TimeSpan(0, 0, 0));
            AssertSerialize(
                "the.counter:5|c|@0.5|#tag1:true,tag2|T1367433000",
                MetricType.Count,
                "the.counter",
                5,
                sampleRate: 0.5,
                tags: new[] { "tag1:true", "tag2" },
                timestamp: dto);
        }

        [Test]
        public void SendCounterWithExternalData()
        {
            AssertSerialize(
                "the.counter:5|c|e:counter-external-data",
                MetricType.Count,
                "the.counter",
                5,
                externalData: "counter-external-data");
        }

        [Test]
        public void SendCounterWithContainerID()
        {
            AssertSerialize(
                "the.counter:5|c|c:container",
                MetricType.Count,
                "the.counter",
                5,
                containerID: "container");
        }

        [Test]
        public void SendCounterWithExternalDataAndContainerID()
        {
            AssertSerialize(
                "the.counter:5|c|c:container|e:counter-external-data",
                MetricType.Count,
                "the.counter",
                5,
                externalData: "counter-external-data",
                containerID: "container");
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

        [Test]
        public void SendTimerWithExternalData()
        {
            AssertSerialize(
                "timer:5|ms|e:timer-external-data",
                MetricType.Timing,
                "timer",
                5,
                externalData: "timer-external-data");
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

        [Test]
        public void SendGaugeWithTimestamp()
        {
            var dto = new DateTimeOffset(2013, 05, 01, 18, 30, 00, new TimeSpan(0, 0, 0));
            AssertSerialize(
                "gauge:5|g|T1367433000",
                MetricType.Gauge,
                "gauge",
                5,
                timestamp: dto);
        }

        [Test]
        public void SendGaugeWithTagsAndTimestamp()
        {
            var dto = new DateTimeOffset(2013, 05, 01, 18, 30, 00, new TimeSpan(0, 0, 0));
            AssertSerialize(
                "gauge:5|g|#tag1:true,tag2|T1367433000",
                MetricType.Gauge,
                "gauge",
                5,
                tags: new[] { "tag1:true", "tag2" },
                timestamp: dto);
        }

        [Test]
        public void SendGaugeWithTagsAndTimestampAndSampleRate()
        {
            var dto = new DateTimeOffset(2013, 05, 01, 18, 30, 00, new TimeSpan(0, 0, 0));
            AssertSerialize(
                "gauge:5|g|@0.5|#tag1:true,tag2|T1367433000",
                MetricType.Gauge,
                "gauge",
                5,
                sampleRate: 0.5,
                tags: new[] { "tag1:true", "tag2" },
                timestamp: dto);
        }

        [Test]
        public void SendGaugeWithExternalData()
        {
            AssertSerialize(
                "gauge:5|g|e:gauge-external-data",
                MetricType.Gauge,
                "gauge",
                5,
                externalData: "gauge-external-data");
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

        [Test]
        public void SendHistogramWithExternalData()
        {
            AssertSerialize(
                "histogram:5|h|e:histogram-external-data",
                MetricType.Histogram,
                "histogram",
                5,
                externalData: "histogram-external-data");
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

        [Test]
        public void SendDistributionWithExternalData()
        {
            AssertSerialize(
                "distribution:5|d|e:distribution-external-data",
                MetricType.Distribution,
                "distribution",
                5,
                externalData: "distribution-external-data");
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

        [Test]
        public void SendSetStringWithExternalData()
        {
            AssertSetSerialize(
                "set:objectname|s|e:set-external-data",
                "set",
                "objectname",
                externalData: "set-external-data");
        }

        [Test]
        public void SendSetStringWithContainerID()
        {
            AssertSetSerialize(
                "set:objectname|s|c:container",
                "set",
                "objectname",
                containerID: "container");
        }

        [Test]
        public void SendSetStringWithExternalDataAndContainerID()
        {
            AssertSetSerialize(
                "set:objectname|s|c:container|e:set-external-data",
                "set",
                "objectname",
                containerID: "container",
                externalData: "set-external-data");
        }

        [Test]
        public void SendCounterWithCardinalityLow()
        {
            AssertSerialize("counter:5|c|card:low", MetricType.Count, "counter", 5, cardinality: Cardinality.Low);
        }

        [Test]
        public void SendCounterWithCardinalityHigh()
        {
            AssertSerialize("counter:5|c|card:high", MetricType.Count, "counter", 5, cardinality: Cardinality.High);
        }

        [Test]
        public void SendCounterWithCardinalityOrchestrator()
        {
            AssertSerialize("counter:5|c|card:orchestrator", MetricType.Count, "counter", 5, cardinality: Cardinality.Orchestrator);
        }

        [Test]
        public void SendCounterWithCardinalityNone()
        {
            AssertSerialize("counter:5|c|card:none", MetricType.Count, "counter", 5, cardinality: Cardinality.None);
        }

        [Test]
        public void SendCounterWithCardinalityAndTags()
        {
            AssertSerialize(
                "counter:5|c|#tag1:true,tag2|card:low",
                MetricType.Count,
                "counter",
                5,
                tags: new[] { "tag1:true", "tag2" },
                cardinality: Cardinality.Low);
        }

        [Test]
        public void SendCounterWithCardinalityAndSampleRate()
        {
            AssertSerialize(
                "counter:5|c|@0.1|card:high",
                MetricType.Count,
                "counter",
                5,
                sampleRate: 0.1,
                cardinality: Cardinality.High);
        }

        [Test]
        public void SendCounterWithCardinalityTagsAndSampleRate()
        {
            AssertSerialize(
                "counter:5|c|@0.1|#tag1:true,tag2|card:orchestrator",
                MetricType.Count,
                "counter",
                5,
                sampleRate: 0.1,
                tags: new[] { "tag1:true", "tag2" },
                cardinality: Cardinality.Orchestrator);
        }

        [Test]
        public void SendCounterWithCardinalityAndTimestamp()
        {
            var dto = new DateTimeOffset(2013, 05, 01, 18, 30, 00, new TimeSpan(0, 0, 0));
            AssertSerialize(
                "counter:5|c|T1367433000|card:low",
                MetricType.Count,
                "counter",
                5,
                timestamp: dto,
                cardinality: Cardinality.Low);
        }

        [Test]
        public void SendGaugeWithCardinalityAndTimestamp()
        {
            var dto = new DateTimeOffset(2013, 05, 01, 18, 30, 00, new TimeSpan(0, 0, 0));
            AssertSerialize(
                "gauge:5|g|T1367433000|card:high",
                MetricType.Gauge,
                "gauge",
                5,
                timestamp: dto,
                cardinality: Cardinality.High);
        }

        [Test]
        public void SendHistogramWithCardinality()
        {
            AssertSerialize("histogram:5|h|card:low", MetricType.Histogram, "histogram", 5, cardinality: Cardinality.Low);
        }

        [Test]
        public void SendDistributionWithCardinality()
        {
            AssertSerialize("distribution:5|d|card:high", MetricType.Distribution, "distribution", 5, cardinality: Cardinality.High);
        }

        [Test]
        public void SendTimerWithCardinality()
        {
            AssertSerialize("timer:5|ms|card:orchestrator", MetricType.Timing, "timer", 5, cardinality: Cardinality.Orchestrator);
        }

        [Test]
        public void SendSetWithCardinality()
        {
            AssertSetSerialize("set:value|s|card:low", "set", "value", cardinality: Cardinality.Low);
        }

        [Test]
        public void SendSetWithCardinalityAndTags()
        {
            AssertSetSerialize(
                "set:value|s|#tag1:true|card:high",
                "set",
                "value",
                tags: new[] { "tag1:true" },
                cardinality: Cardinality.High);
        }

        [Test]
        public void SendSetWithCardinalityAndSampleRate()
        {
            AssertSetSerialize(
                "set:value|s|@0.5|card:none",
                "set",
                "value",
                sampleRate: 0.5,
                cardinality: Cardinality.None);
        }

        private static void AssertSerialize(
            string expectValue,
            MetricType metricType,
            string name,
            double value,
            double sampleRate = 1.0,
            string[] tags = null,
            string prefix = null,
            DateTimeOffset? timestamp = null,
            string externalData = null,
            string containerID = null,
            Cardinality? cardinality = null)
        {
            var statsMetric = new StatsMetric
            {
                MetricType = metricType,
                StatName = name,
                SampleRate = sampleRate,
                NumericValue = value,
                Tags = tags,
                Cardinality = cardinality,
            };
            if (timestamp != null)
            {
                statsMetric.Timestamp = timestamp.Value.ToUnixTimeSeconds();
            }

            AssertSerialize(expectValue, ref statsMetric, prefix, externalData, containerID);
        }

        private static void AssertSetSerialize(
           string expectValue,
           string name,
           object value,
           double sampleRate = 1.0,
           string[] tags = null,
           string prefix = null,
           string externalData = null,
           string containerID = null,
           Cardinality? cardinality = null)
        {
            var statsMetric = new StatsMetric
            {
                MetricType = MetricType.Set,
                StatName = name,
                SampleRate = sampleRate,
                StringValue = value.ToString(),
                Tags = tags,
                Cardinality = cardinality,
            };
            AssertSerialize(expectValue, ref statsMetric, prefix, externalData, containerID);
        }

        private static void AssertSerialize(
           string expectValue,
           ref StatsMetric statsMetric,
           string prefix,
           string externalData,
           string containerID)
        {
            var originDetection = new OriginDetection(externalData, containerID);
            var serializerHelper = new SerializerHelper(null, originDetection);
            var serializer = new MetricSerializer(serializerHelper, prefix);
            var serializedMetric = new SerializedMetric();
            serializer.SerializeTo(ref statsMetric, serializedMetric);
            Assert.AreEqual(expectValue, serializedMetric.ToString());
        }
    }
}
