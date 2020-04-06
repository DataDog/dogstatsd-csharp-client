using System;
using System.Threading.Tasks;

namespace StatsdClient.Worker
{
    internal class Waiter : IWaiter
    {
        public void Wait(TimeSpan duration)
        {
            Task.Delay(duration).Wait();
        }
    }
}