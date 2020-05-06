using System;

namespace StatsdClient.Worker
{
    internal interface IManualResetEvent
    {
        bool Wait(TimeSpan duration);

        void Set();

        void Reset();
    }
}