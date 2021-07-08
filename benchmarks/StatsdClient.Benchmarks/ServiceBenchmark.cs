using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace StatsdClient.Benchmarks
{
    public class ServiceBenchmark
    {
        private DogStatsdService _service;
        static readonly string[] tags = new[] { "TAG1", "TAG2", "TAG3" };
        const int iterationCount = 10 * 1000 * 1000;

        public IEnumerable<(int, TimeSpan?)> ParamsValues => new[] {
            ValueTuple.Create<int, TimeSpan?>(iterationCount, null),
            ValueTuple.Create<int, TimeSpan?>(iterationCount, TimeSpan.FromMinutes(1)),
            ValueTuple.Create<int, TimeSpan?>(10 * 1024, TimeSpan.FromMinutes(1)),
            };

        [ParamsSource(nameof(ParamsValues))]
        public (int MaxMetricsInAsyncQueue, TimeSpan? MaxBlockDuration) Params { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _service = new DogStatsdService();
            var config = new StatsdConfig
            {
                StatsdServerName = "127.0.0.1",
                StatsdPort = 1234, // Invalid port, UDP payloads are dropped.
                StatsdMaxUDPPacketSize = 8096
            };
            // Disable telemetry
            config.Advanced.TelemetryFlushInterval = TimeSpan.FromDays(1);

            config.Advanced.MaxBlockDuration = this.Params.MaxBlockDuration;
            config.Advanced.MaxMetricsInAsyncQueue = this.Params.MaxMetricsInAsyncQueue;
            _service.Configure(config);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _service.Dispose();
        }

        [Benchmark]
        public void Increment()
        {
            for (int i = 0; i < iterationCount; ++i)
            {
                _service.Increment("statsd_client.benchmarks.test_counter", 1, tags: tags);
            }
            _service.Flush(flushTelemetry: false);
            var counters = _service.TelemetryCounters;

            if (counters.PacketsDropped > 0 || counters.PacketsDroppedQueue > 0)
            {
                throw new InvalidOperationException("Invalid benchmark. Packets dropped.");
            }
        }
    }
}