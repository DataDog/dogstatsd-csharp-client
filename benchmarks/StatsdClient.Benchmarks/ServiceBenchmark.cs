using System;
using BenchmarkDotNet.Attributes;

namespace StatsdClient.Benchmarks
{
    public class ServiceBenchmark
    {
        private DogStatsdService _service;
        static readonly string[] tags = new[] { "TAG1", "TAG2", "TAG3" };
        const int iterationCount = 10 * 1000 * 1000;

        // Because of the issue https://github.com/dotnet/BenchmarkDotNet/issues/848,
        // we need to have one GlobalSetup by Benchmark.

        [GlobalSetup(Target = nameof(NonBlockingQueue))]
        public void GlobalSetup1() => GlobalSetup(iterationCount, null);

        [Benchmark(Description = "Non blocking queue with high capacity")]
        public void NonBlockingQueue() => Increment(); // Use GlobalSetup1


        [GlobalSetup(Target = nameof(BlockingQueue))]
        public void GlobalSetup2() => GlobalSetup(iterationCount, TimeSpan.FromMinutes(1));

        [Benchmark(Description = "Blocking queue with high capacity")]
        public void BlockingQueue() => Increment();


        [GlobalSetup(Target = nameof(BlockingQueueLowCapacity))]
        public void GlobalSetup3() => GlobalSetup(10 * 1024, TimeSpan.FromMinutes(1));

        [Benchmark(Baseline = true, Description = "Blocking queue with low capacity")]
        public void BlockingQueueLowCapacity() => Increment();


        [GlobalSetup(Target = nameof(ClientSideAggregation))]
        public void GlobalSetup4() => GlobalSetup(10 * 1024, TimeSpan.FromMinutes(1), clientSideAggregationConfig: true);

        [Benchmark(Description = "Blocking queue with low capacity with aggregation")]
        public void ClientSideAggregation() => Increment();


        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _service.Dispose();
        }

        private void Increment()
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

        private void GlobalSetup(
            int maxMetricsInAsyncQueue,
            TimeSpan? maxBlockDuration,
            bool clientSideAggregationConfig = false)
        {
            _service = new DogStatsdService();
            var config = new StatsdConfig
            {
                StatsdServerName = "127.0.0.1",
                StatsdPort = 1234, // Invalid port, UDP payloads are dropped.
                StatsdMaxUDPPacketSize = 8096
            };
            // Do not send the telemetry but create an instance of `Telemetry` to gets 
            // the values of `TelemetryCounters` in Increment().
            config.Advanced.TelemetryFlushInterval = TimeSpan.FromDays(1);

            config.Advanced.MaxBlockDuration = maxBlockDuration;
            config.Advanced.MaxMetricsInAsyncQueue = maxMetricsInAsyncQueue;
            if (clientSideAggregationConfig)
            {
                config.ClientSideAggregation = new ClientSideAggregationConfig();
            }

            _service.Configure(config);
        }
    }
}