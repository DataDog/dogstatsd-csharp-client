using System;

namespace StatsdClient.Worker
{
    interface IWaiter
    {
        void Wait(TimeSpan duration);
    }
}