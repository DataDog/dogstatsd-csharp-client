using System;

namespace StatsdClient
{
    [ObsoleteAttribute("This interface will become private in a future release.")]
    public interface IStopwatch
    {
        void Start();
        void Stop();
        int ElapsedMilliseconds();
    }
}