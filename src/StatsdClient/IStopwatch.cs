namespace StatsdClient
{
    internal interface IStopwatch
    {
        void Start();

        void Stop();

        int ElapsedMilliseconds();
    }
}