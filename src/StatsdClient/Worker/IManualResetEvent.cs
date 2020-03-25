using System;

namespace StatsdClient.Worker
{
    interface IManualResetEvent
    {
        bool Wait(TimeSpan duration);

        void Set();

        void Reset();
    }
}