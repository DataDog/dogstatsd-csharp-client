using System;

namespace StatsdClient.Worker
{
    internal class Waiter : IWaiter
    {
        public void Wait(TimeSpan duration)
        {
#if NETSTANDARD1_3
            System.Threading.Tasks.Task.Delay(duration).Wait();
#else
            System.Threading.Thread.Sleep(duration);
#endif
        }
    }
}