using System;
using System.Net;
using Mono.Unix;
using StatsdClient.Aggregator;
using StatsdClient.Transport;

namespace StatsdClient.Bufferize
{
    /// <summary>
    /// IStatsBufferizeFactory is a factory for StatsBufferize.
    /// It is used to test StatsBufferize.
    /// </summary>
    internal interface IStatsBufferizeFactory
    {
        StatsBufferize CreateStatsBufferize(
          StatsRouter statsRouter,
          int workerMaxItemCount,
          TimeSpan? blockingQueueTimeout,
          TimeSpan maxIdleWaitBeforeSending,
          Action<Exception> optionalExceptionHandler);

        StatsRouter CreateStatsRouter(
            Serializers serializers,
            BufferBuilder bufferBuilder,
            Aggregators optionalAggregators);

        ITransport CreateUDPTransport(IPEndPoint endPoint);

        ITransport CreateUnixDomainSocketTransport(
            UnixEndPoint endPoint,
            TimeSpan? udsBufferFullBlockDuration);

        ITransport CreateNamedPipeTransport(string pipeName);

        Telemetry CreateTelemetry(
            MetricSerializer metricSerializer,
            string assemblyVersion,
            TimeSpan flushInterval,
            ITransport transport,
            string[] globalTags,
            Action<Exception> optionalExceptionHandler);
    }
}