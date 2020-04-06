using System;

namespace StatsdClient.Worker
{
    internal interface IWaiter
    {
        void Wait(TimeSpan duration);
    }
}