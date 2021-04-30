using BenchmarkDotNet.Attributes;
using StatsdClient;

namespace StatsdClient.Benchmarks
{
    public class ServiceBenchmark
    {
        private DogStatsdService service;
        static readonly string[] tags = new[] { "TAG1", "TAG2", "TAG3" };
        const int iterationCount = 20 * 1000 * 1000;

        [GlobalSetup]
        public void GlobalSetup()
        {
            service = new DogStatsdService();
            var config = new StatsdConfig
            {
                StatsdServerName = "127.0.0.1",
                StatsdPort = 1234, // Invalid port, UDP payloads are dropped.
                StatsdMaxUDPPacketSize = 8096
            };
            // Disable telemetry
            config.Advanced.TelemetryFlushInterval = null;

            // Make sure metrics are never dropped.
            config.Advanced.MaxMetricsInAsyncQueue = iterationCount;
            service.Configure(config);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            service.Dispose();
        }

        [Benchmark]
        public void Increment()
        {
            for (int i = 0; i < iterationCount; ++i)
            {
                service.Increment("statsd_client.benchmarks.test_counter", 1, tags: tags);
            }
            service.Flush();
        }
    }
}