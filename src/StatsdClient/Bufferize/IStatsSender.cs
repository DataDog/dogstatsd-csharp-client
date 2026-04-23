using System;
using StatsdClient.Statistic;

namespace StatsdClient.Bufferize
{
    /// <summary>
    /// IStatsSender defines the contract for sending stats to the pipeline.
    /// </summary>
    internal interface IStatsSender : IDisposable
    {
        bool TryDequeueFromPool(out Stats stats);

        void Send(Stats stats);

        void Flush();
    }
}
