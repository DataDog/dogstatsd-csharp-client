using System;
using System.Net;
using Mono.Unix;
using StatsdClient.Aggregator;
using StatsdClient.Transport;

namespace StatsdClient.Bufferize
{
    internal class StatsBufferizeFactory : IStatsBufferizeFactory
    {
        public StatsBufferize CreateStatsBufferize(
            StatsRouter statsRouter,
            int workerMaxItemCount,
            TimeSpan? blockingQueueTimeout,
            TimeSpan maxIdleWaitBeforeSending)
        {
            return new StatsBufferize(
                statsRouter,
                workerMaxItemCount,
                blockingQueueTimeout,
                maxIdleWaitBeforeSending);
        }

        public StatsRouter CreateStatsRouter(
            Serializers serializers,
            BufferBuilder bufferBuilder,
            Aggregators optionalAggregators)
        {
            return new StatsRouter(serializers, bufferBuilder, optionalAggregators);
        }

        public ITransport CreateUDPTransport(IPEndPoint endPoint)
        {
            return new UDPTransport(endPoint);
        }

        public ITransport CreateUnixDomainSocketTransport(
            UnixEndPoint endPoint,
            TimeSpan? udsBufferFullBlockDuration)
        {
            return new UnixDomainSocketTransport(endPoint, udsBufferFullBlockDuration);
        }

        public Telemetry CreateTelemetry(MetricSerializer metricSerializer, string assemblyVersion, TimeSpan flushInterval, ITransport transport, string[] globalTags)
        {
            return new Telemetry(metricSerializer, assemblyVersion, flushInterval, transport, globalTags);
        }

        public ITransport CreateNamedPipeTransport(string pipeName)
        {
            return new NamedPipeTransport(pipeName);
        }
    }
}