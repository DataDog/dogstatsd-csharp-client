using System;
using System.Net;
using Mono.Unix;
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
          TimeSpan maxIdleWaitBeforeSending);

        StatsRouter CreateStatsRouter(
            Serializers serializers,
            BufferBuilder bufferBuilder);

        ITransport CreateUDPTransport(IPEndPoint endPoint);

        ITransport CreateUnixDomainSocketTransport(
            UnixEndPoint endPoint,
            TimeSpan? udsBufferFullBlockDuration);

        ITransport CreateNamedPipeTransport(string pipeName);

        Telemetry CreateTelemetry(
            string assemblyVersion,
            TimeSpan flushInterval,
            ITransport transport,
            string[] globalTags);
    }
}