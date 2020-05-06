using System;

namespace StatsdClient
{
    [ObsoleteAttribute("This class will become private in a future release.")]
    public class StopWatchFactory : IStopWatchFactory
    {
        public IStopwatch Get()
        {
            return new Stopwatch();
        }
    }
}