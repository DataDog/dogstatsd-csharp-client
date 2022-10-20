using System;

namespace StatsdClient.Worker
{
    internal class Waiter : IWaiter
    {
        public void Wait(TimeSpan duration)
        {
            System.Threading.Thread.Sleep(duration);
        }
    }
}