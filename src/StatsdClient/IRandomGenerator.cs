using System;

namespace StatsdClient
{
    [ObsoleteAttribute("This interface will become private in a future release.")]
    public interface IRandomGenerator
    {
        bool ShouldSend(double sampleRate);
    }
}