using System;
using StatsdClient.Statistic;

namespace StatsdClient.Bufferize
{
    /// <summary>
    /// IStatsBufferize defines the contract for sending stats to the pipeline.
    /// </summary>
    internal interface IStatsBufferize : IDisposable
    {
        bool TryDequeueFromPool(out Stats stats);

        void Send(Stats stats);

        void Flush();
    }
}
