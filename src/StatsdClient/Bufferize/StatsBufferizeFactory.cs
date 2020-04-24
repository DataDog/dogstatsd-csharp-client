using System;
using System.Net;
using Mono.Unix;

namespace StatsdClient.Bufferize
{
    class StatsBufferizeFactory : IStatsBufferizeFactory
    {
        public StatsBufferize CreateStatsBufferize(BufferBuilder bufferBuilder,
                                                   int workerMaxItemCount,
                                                   TimeSpan? blockingQueueTimeout,
                                                   TimeSpan maxIdleWaitBeforeSending)
        {
            return new StatsBufferize(bufferBuilder,
                                      workerMaxItemCount,
                                      blockingQueueTimeout,
                                      maxIdleWaitBeforeSending);
        }

        public StatsSender CreateUDPStatsSender(IPEndPoint endPoint)
        {
            return StatsSender.CreateUDPStatsSender(endPoint);
        }

        public StatsSender CreateUnixDomainSocketStatsSender(UnixEndPoint endPoint,
                                                             TimeSpan? udsBufferFullBlockDuration)
        {
            return StatsSender.CreateUnixDomainSocketStatsSender(endPoint, udsBufferFullBlockDuration);
        }
    }
}