using System;
using System.Net;
using Mono.Unix;

namespace StatsdClient.Bufferize
{
    internal class StatsBufferizeFactory : IStatsBufferizeFactory
    {
        public StatsBufferize CreateStatsBufferize(
            Telemetry telemetry,
            BufferBuilder bufferBuilder,
            int workerMaxItemCount,
            TimeSpan? blockingQueueTimeout,
            TimeSpan maxIdleWaitBeforeSending)
        {
            return new StatsBufferize(
                telemetry,
                bufferBuilder,
                workerMaxItemCount,
                blockingQueueTimeout,
                maxIdleWaitBeforeSending);
        }

        public IStatsSender CreateUDPStatsSender(IPEndPoint endPoint)
        {
            return new UDPTransport(endPoint);
        }

        public IStatsSender CreateUnixDomainSocketStatsSender(
            UnixEndPoint endPoint,
            TimeSpan? udsBufferFullBlockDuration)
        {
            return new UnixDomainSocketTransport(endPoint, udsBufferFullBlockDuration);
        }

        public Telemetry CreateTelemetry(string assemblyVersion, TimeSpan flushInterval, IStatsSender statsSender, string[] globalTags)
        {
            return new Telemetry(assemblyVersion, flushInterval, statsSender, globalTags);
        }
    }
}