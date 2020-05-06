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

        public StatsSender CreateUDPStatsSender(IPEndPoint endPoint)
        {
            return StatsSender.CreateUDPStatsSender(endPoint);
        }

        public StatsSender CreateUnixDomainSocketStatsSender(
            UnixEndPoint endPoint,
            TimeSpan? udsBufferFullBlockDuration)
        {
            return StatsSender.CreateUnixDomainSocketStatsSender(endPoint, udsBufferFullBlockDuration);
        }
    }
}