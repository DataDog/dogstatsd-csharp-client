namespace StatsdClient
{
    internal class StopWatchFactory : IStopWatchFactory
    {
        public IStopwatch Get()
        {
            return new Stopwatch();
        }
    }
}