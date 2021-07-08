using System;
using BenchmarkDotNet.Attributes;
using StatsdClient.Bufferize;
using StatsdClient.Statistic;

namespace StatsdClient.Benchmarks
{
    [MemoryDiagnoser]
    public class BufferBuilderBenchmark
    {
        private BufferBuilder _bufferBuilder;
        private SerializedMetric _serializedMetric;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _serializedMetric = new SerializedMetric();
            _serializedMetric.Builder.Append(new String('A', 128));
            _bufferBuilder = new BufferBuilder(new EmptyHandler(), 8096, "\n");
        }
        class EmptyHandler : IBufferBuilderHandler
        {
            public void Handle(byte[] buffer, int length) { }
        }

        [Benchmark]
        public void Add()
        {
            _bufferBuilder.HandleBufferAndReset();
            for (int i = 0; i < 1000 * 1000; i++)
            {
                _bufferBuilder.Add(_serializedMetric);
            }
        }
    }
}