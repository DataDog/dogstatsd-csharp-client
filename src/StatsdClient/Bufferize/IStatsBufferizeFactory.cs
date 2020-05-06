using System;
using System.Net;
using Mono.Unix;

namespace StatsdClient.Bufferize
{
    /// <summary>
    /// IStatsBufferizeFactory is a factory for StatsBufferize.
    /// It is used to test StatsBufferize.
    /// </summary>
    internal interface IStatsBufferizeFactory
    {
        StatsBufferize CreateStatsBufferize(
          Telemetry telemetry,
          BufferBuilder bufferBuilder,
          int workerMaxItemCount,
          TimeSpan? blockingQueueTimeout,
          TimeSpan maxIdleWaitBeforeSending);

        StatsSender CreateUDPStatsSender(IPEndPoint endPoint);

        StatsSender CreateUnixDomainSocketStatsSender(
            UnixEndPoint endPoint,
            TimeSpan? udsBufferFullBlockDuration);

        Telemetry CreateTelemetry(
            string assemblyVersion,
            TimeSpan flushInterval,
            IStatsSender statsSender,
            string[] globalTags);
    }
}